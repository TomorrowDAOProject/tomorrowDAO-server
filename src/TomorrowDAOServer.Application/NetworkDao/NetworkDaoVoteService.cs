using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.NetworkDao;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.GrainDtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoVoteService : TomorrowDAOServerAppService, INetworkDaoVoteService
{
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IUserProvider _userProvider;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;
    private readonly ITokenService _tokenService;
    private readonly ILogger<NetworkDaoVoteService> _logger;

    private const string ElfDecimal = "8";

    public NetworkDaoVoteService(INetworkDaoEsDataProvider networkDaoEsDataProvider, IObjectMapper objectMapper,
        IClusterClient clusterClient, IUserProvider userProvider, IExplorerProvider explorerProvider,
        IOptionsMonitor<TelegramOptions> telegramOptions, ITokenService tokenService,
        ILogger<NetworkDaoVoteService> logger)
    {
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _userProvider = userProvider;
        _explorerProvider = explorerProvider;
        _telegramOptions = telegramOptions;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<GetVotedListPagedResult> GetVotedListAsync(GetVotedListInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
        {
            return new GetVotedListPagedResult();
        }

        var (count, voteIndices) = await _networkDaoEsDataProvider.GetProposalVotedListAsync(input);
        if (voteIndices.IsNullOrEmpty())
        {
            return new GetVotedListPagedResult
            {
                Items = new List<GetVotedListResultDto>(),
                TotalCount = count
            };
        }

        var symbols = voteIndices.Select(t => t.Symbol).Distinct().ToList();
        var tokenInfos = new Dictionary<string, TokenInfoDto>();
        foreach (var symbol in symbols)
        {
            tokenInfos[symbol] = await _tokenService.GetTokenInfoAsync(input.ChainId, symbol);
        }

        var voteAmount = new Dictionary<string, decimal>();
        foreach (var voteIndex in voteIndices)
        {
            var tokenSymbol = voteIndex.Symbol.IsNullOrWhiteSpace() || voteIndex.Symbol == "none"
                ? string.Empty
                : voteIndex.Symbol;
            if (tokenSymbol.IsNullOrWhiteSpace() || !tokenInfos.ContainsKey(tokenSymbol))
            {
                _logger.LogWarning("Token information not found. {0}", tokenSymbol);
                continue;
            }

            var symbolDecimal = tokenInfos[tokenSymbol].Decimals;
            if (voteIndex.TransactionInfo?.ChainId == null || voteIndex.TransactionInfo.ChainId.IsNullOrWhiteSpace())
            {
                symbolDecimal = ElfDecimal;
            }

            if (!int.TryParse(symbolDecimal, out var decimalIntValue) || decimalIntValue <= 0)
            {
                continue;
            }

            var pow = Math.Pow(10, decimalIntValue);
            voteAmount[voteIndex.Id] = voteIndex.Amount / (decimal)pow;
        }

        var resultDtos = _objectMapper.Map<List<NetworkDaoProposalVoteIndex>, List<GetVotedListResultDto>>(voteIndices);

        foreach (var resultDto in resultDtos)
        {
            resultDto.Amount = voteAmount.GetValueOrDefault(resultDto.Id, resultDto.Amount);
        }

        return new GetVotedListPagedResult
        {
            Items = resultDtos,
            TotalCount = count
        };
    }

    public async Task<GetAllPersonalVotesPagedResult> GetAllPersonalVotesAsync(GetAllPersonalVotesInput input)
    {
        var (totalCount, voteIndices) = await _networkDaoEsDataProvider.GetProposalVotedListAsync(new GetVotedListInput
        {
            MaxResultCount = input.MaxResultCount,
            SkipCount = input.SkipCount,
            Sorting = input.Sorting,
            ChainId = input.ChainId,
            ProposalId = input.Search,
            ProposalType = input.ProposalType,
            Address = input.Address
        });
        var resultDtos = new List<GetAllPersonalVotesResultDto>();
        if (!voteIndices.IsNullOrEmpty())
        {
            resultDtos =
                _objectMapper.Map<List<NetworkDaoProposalVoteIndex>, List<GetAllPersonalVotesResultDto>>(voteIndices);
        }

        return new GetAllPersonalVotesPagedResult
        {
            Items = resultDtos,
            TotalCount = totalCount
        };
    }

    public async Task<AddTeamDescResultDto> AddTeamDescriptionAsync(AddTeamDescInput input, bool authRequired = true)
    {
        if (authRequired)
        {
            var address =
                await _userProvider.GetAndValidateUserAddressAsync(
                    CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
            if (address.IsNullOrEmpty())
            {
                return new AddTeamDescResultDto
                {
                    Success = false,
                    Message = "No user address found"
                };
            }
        }

        var voteTeamDto = _objectMapper.Map<AddTeamDescInput, NetworkDaoVoteTeamDto>(input);
        voteTeamDto.Id = IdGeneratorHelper.GenerateId(input.ChainId, input.Name, input.PublicKey);

        var voteTeamGrain = _clusterClient.GetGrain<INetworkDaoVoteTeamGrain>(input.Name);
        var resultDto = await voteTeamGrain.SaveVoteTeamDescriptionAsync(voteTeamDto);

        if (!resultDto.Success)
        {
            return new AddTeamDescResultDto
            {
                Success = false,
                Message = resultDto.Message
            };
        }

        var voteTeamIndex = _objectMapper.Map<AddTeamDescInput, NetworkDaoVoteTeamIndex>(input);
        voteTeamIndex.Id = voteTeamDto.Id;
        var now = DateTime.Now;
        voteTeamIndex.CreateTime = now;
        voteTeamIndex.UpdateTime ??= now;
        await _networkDaoEsDataProvider.BulkAddOrUpdateVoteTeamAsync(new List<NetworkDaoVoteTeamIndex>()
            { voteTeamIndex });

        return new AddTeamDescResultDto
        {
            Success = true
        };
    }

    public async Task<UpdateTeamStatusResultDto> UpdateTeamStatusAsync(UpdateTeamStatusInput input)
    {
        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (address.IsNullOrEmpty())
        {
            return new UpdateTeamStatusResultDto
            {
                Success = false,
                Message = "No user address found"
            };
        }

        var voteTeamGrain = _clusterClient.GetGrain<INetworkDaoVoteTeamGrain>(input.Name);
        var resultDto = await voteTeamGrain.UpdateVoteTeamStatusAsync(input.PublicKey, input.IsActive);
        if (!resultDto.Success)
        {
            return new UpdateTeamStatusResultDto
            {
                Success = false,
                Message = resultDto.Message
            };
        }

        //Grain updated the data with the specified name, and Index updated all the data under the publickey
        var (count, voteTeamIndices) = await _networkDaoEsDataProvider.GetVoteTeamListAsync(new GetVoteTeamListInput
        {
            ChainId = input.ChainId,
            PublicKey = input.PublicKey
        });
        foreach (var voteTeamIndex in voteTeamIndices)
        {
            voteTeamIndex.IsActive = input.IsActive;
            voteTeamIndex.UpdateTime = DateTime.Now;
        }

        await _networkDaoEsDataProvider.BulkAddOrUpdateVoteTeamAsync(voteTeamIndices);

        return new UpdateTeamStatusResultDto
        {
            Success = true
        };
    }

    public async Task<int> LoadVoteTeamHistoryDateAsync(LoadVoteTeamDescHistoryInput input)
    {
        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (address.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Access denied");
        }

        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        var count = 0;
        var allTeamDesc = await _explorerProvider.GetAllTeamDescAsync(input.OperateChainId, false);
        foreach (var teamDescDto in allTeamDesc)
        {
            count++;
            var teamDescInput = _objectMapper.Map<ExplorerVoteTeamDescDto, AddTeamDescInput>(teamDescDto);
            teamDescInput.ChainId = input.OperateChainId;
            await AddTeamDescriptionAsync(teamDescInput, false);
        }

        allTeamDesc = await _explorerProvider.GetAllTeamDescAsync(input.OperateChainId, true);
        foreach (var teamDescDto in allTeamDesc)
        {
            count++;
            var teamDescInput = _objectMapper.Map<ExplorerVoteTeamDescDto, AddTeamDescInput>(teamDescDto);
            teamDescInput.ChainId = input.OperateChainId;
            await AddTeamDescriptionAsync(teamDescInput, false);
        }

        return count;
    }

    public async Task<GetTeamDescResultDto> GetTeamDescAsync(GetTeamDescInput input)
    {
        var (count, voteTeamIndices) =
            await _networkDaoEsDataProvider.GetVoteTeamListAsync(new GetVoteTeamListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                ChainId = input.ChainId,
                PublicKey = input.PublicKey
            });
        if (voteTeamIndices.IsNullOrEmpty())
        {
            throw new UserFriendlyException("not found");
        }

        return _objectMapper.Map<NetworkDaoVoteTeamIndex, GetTeamDescResultDto>(voteTeamIndices.First());
    }

    public async Task<List<GetTeamDescResultDto>> GetAllTeamDescAsync(GetAllTeamDescInput input)
    {
        var (count, voteTeamIndices) =
            await _networkDaoEsDataProvider.GetVoteTeamListAsync(new GetVoteTeamListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                ChainId = input.ChainId,
                IsActive = input.IsActive
            });

        if (voteTeamIndices.IsNullOrEmpty())
        {
            return new List<GetTeamDescResultDto>();
        }

        voteTeamIndices = voteTeamIndices.GroupBy(t => t.PublicKey).Select(g => g.First()).ToList();
        return _objectMapper.Map<List<NetworkDaoVoteTeamIndex>, List<GetTeamDescResultDto>>(voteTeamIndices);
    }

    public async Task<Dictionary<string, NetworkDaoProposalVoteIndex>> GetPersonVotedDictionaryAsync(string chainId,
        string address, List<string> proposalIds)
    {
        if (address.IsNullOrWhiteSpace())
        {
            return new Dictionary<string, NetworkDaoProposalVoteIndex>();
        }

        var (totalCount, votedList) = await _networkDaoEsDataProvider.GetProposalVotedListAsync(new GetVotedListInput
        {
            MaxResultCount = GetVotedListInput.MaxMaxResultCount,
            ChainId = chainId,
            ProposalIds = proposalIds,
            Address = address
        });
        votedList ??= new List<NetworkDaoProposalVoteIndex>();
        return votedList.GroupBy(t => t.ProposalId).ToDictionary(t => t.Key, t => t.FirstOrDefault());
    }
}