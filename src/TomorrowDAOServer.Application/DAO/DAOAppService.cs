using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.DAO.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.User.Provider;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace TomorrowDAOServer.DAO;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class DAOAppService : ApplicationService, IDAOAppService
{
    private readonly ILogger<DAOAppService> _logger;
    private readonly IDAOProvider _daoProvider;
    private readonly IElectionProvider _electionProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IOptionsMonitor<DaoOptions> _testDaoOptions;
    private readonly IGovernanceProvider _governanceProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IUserProvider _userProvider;
    private readonly ITokenService _tokenService;

    public DAOAppService(IDAOProvider daoProvider, IElectionProvider electionProvider,
        IGovernanceProvider governanceProvider,
        IProposalProvider proposalProvider, IExplorerProvider explorerProvider, IGraphQLProvider graphQlProvider,
        IObjectMapper objectMapper, IOptionsMonitor<DaoOptions> testDaoOptions, IContractProvider contractProvider,
        IUserProvider userProvider, ILogger<DAOAppService> logger, ITokenService tokenService)
    {
        _daoProvider = daoProvider;
        _electionProvider = electionProvider;
        _proposalProvider = proposalProvider;
        _graphQlProvider = graphQlProvider;
        _objectMapper = objectMapper;
        _testDaoOptions = testDaoOptions;
        _contractProvider = contractProvider;
        _userProvider = userProvider;
        _logger = logger;
        _tokenService = tokenService;
        _explorerProvider = explorerProvider;
        _governanceProvider = governanceProvider;
    }

    public async Task<DAOInfoDto> GetDAOByIdAsync(GetDAOInfoInput input)
    {
        var daoIndex = await _daoProvider.GetAsync(input);
        if (daoIndex == null)
        {
            return new DAOInfoDto();
        }

        input.DAOId = daoIndex.Id;

        var sw = Stopwatch.StartNew();

        var getTreasuryAddressTask = _contractProvider.GetTreasuryAddressAsync(input.ChainId, input.DAOId);
        var getGovernanceSchemeTask = _governanceProvider.GetGovernanceSchemeAsync(input.ChainId, input.DAOId);

        Task<BpInfoDto> getBpWithRoundTask = null;
        Task<List<string>> getHighCouncilMembersTask = null;
        if (daoIndex.IsNetworkDAO)
        {
            getBpWithRoundTask = _graphQlProvider.GetBPWithRoundAsync(input.ChainId);
        }
        else
        {
            getHighCouncilMembersTask = _electionProvider.GetHighCouncilMembersAsync(input.ChainId, input.DAOId);
        }

        var daoInfo = _objectMapper.Map<DAOIndex, DAOInfoDto>(daoIndex);
        if (daoInfo.TreasuryContractAddress.IsNullOrWhiteSpace())
        {
            daoInfo.TreasuryContractAddress =
                _contractProvider.ContractAddress(input.ChainId, CommonConstant.TreasuryContractAddressName);
        }

        if (daoIndex.IsNetworkDAO)
        {
            await Task.WhenAll(getTreasuryAddressTask, getGovernanceSchemeTask, getBpWithRoundTask);
            var bpInfo = getBpWithRoundTask.Result;
            daoInfo.HighCouncilTermNumber = bpInfo.Round;
            daoInfo.HighCouncilMemberCount = bpInfo.AddressList.Count;
        }
        else
        {
            await Task.WhenAll(getTreasuryAddressTask, getGovernanceSchemeTask, getHighCouncilMembersTask);
            daoInfo.HighCouncilMemberCount = getHighCouncilMembersTask.Result.IsNullOrEmpty()
                ? 0
                : getHighCouncilMembersTask.Result.Count;
        }

        daoInfo.TreasuryAccountAddress = getTreasuryAddressTask.Result;
        var governanceSchemeDto = getGovernanceSchemeTask.Result;
        daoInfo.OfGovernanceSchemeThreshold(governanceSchemeDto.Data?.FirstOrDefault());

        sw.Stop();
        _logger.LogInformation("GetDAOByIdDuration: Parallel exec {0}", sw.ElapsedMilliseconds);

        return daoInfo;
    }

    public async Task<PageResultDto<MemberDto>> GetMemberListAsync(GetMemberListInput input)
    {
        if (input == null || (input.DAOId.IsNullOrWhiteSpace() && input.Alias.IsNullOrWhiteSpace()))
        {
            ExceptionHelper.ThrowArgumentException();
        }

        try
        {
            if (input.DAOId.IsNullOrWhiteSpace())
            {
                var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
                {
                    ChainId = input.ChainId,
                    DAOId = input.DAOId,
                    Alias = input.Alias
                });
                if (daoIndex == null || daoIndex.Id.IsNullOrWhiteSpace())
                {
                    throw new UserFriendlyException("No DAO information found.");
                }

                input.DAOId = daoIndex.Id;
            }

            return await _daoProvider.GetMemberListAsync(input);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetMemberListAsync error, {0}", JsonConvert.SerializeObject(input));
            throw new UserFriendlyException($"System exception occurred during querying member list. {e.Message}");
        }
    }

    public async Task<PagedResultDto<DAOListDto>> GetDAOListAsync(QueryDAOListInput input)
    {
        var daoOption = _testDaoOptions.CurrentValue;
        var begin = input.SkipCount;
        var end = begin + input.MaxResultCount;
        var topCount = daoOption.TopDaoNames.Count;
        var excludeNames = new HashSet<string>(daoOption.FilteredDaoNames.Union(daoOption.TopDaoNames));
        Tuple<long, List<DAOListDto>> normalSearch;
        if (begin >= topCount)
        {
            input.SkipCount -= topCount;
            normalSearch = await GetNormalSearchList(input, excludeNames);
            return new PagedResultDto<DAOListDto>
            {
                TotalCount = topCount + normalSearch.Item1,
                Items = normalSearch.Item2
            };
        }

        Tuple<long, List<DAOListDto>> nameSearch;
        if (end <= topCount)
        {
            nameSearch = await GetNameSearchList(input, daoOption.TopDaoNames.Skip(begin).Take(end - begin).ToList());
            var normalCount = await _daoProvider.GetDAOListCountAsync(input, excludeNames);
            return new PagedResultDto<DAOListDto>
            {
                TotalCount = topCount + normalCount,
                Items = nameSearch.Item2
            };
        }

        nameSearch = await GetNameSearchList(input, daoOption.TopDaoNames.Skip(begin).Take(topCount - begin).ToList());
        input.SkipCount = 0;
        input.MaxResultCount = end - topCount;
        normalSearch = await GetNormalSearchList(input, excludeNames);
        var combineList = new List<DAOListDto>();
        combineList.AddRange(nameSearch.Item2);
        combineList.AddRange(normalSearch.Item2);
        return new PagedResultDto<DAOListDto>
        {
            TotalCount = topCount + normalSearch.Item1,
            Items = combineList
        };
    }

    private async Task<Tuple<long, List<DAOListDto>>> GetNormalSearchList(QueryDAOListInput input,
        ISet<string> excludeNames)
    {
        return await FillDaoListAsync(input.ChainId,
            await _daoProvider.GetDAOListAsync(input, excludeNames));
    }

    private async Task<Tuple<long, List<DAOListDto>>> GetNameSearchList(QueryDAOListInput input,
        List<string> searchNames)
    {
        var result = await FillDaoListAsync(input.ChainId,
            await _daoProvider.GetDAOListByNameAsync(input.ChainId, searchNames));
        return new Tuple<long, List<DAOListDto>>(result.Item1,
            result.Item2.OrderBy(x => searchNames.IndexOf(x.Name)).ToList());
    }

    private async Task<Tuple<long, List<DAOListDto>>> FillDaoListAsync(string chainId,
        Tuple<long, List<DAOIndex>> originResult)
    {
        var daoList = originResult.Item2;
        var items = _objectMapper.Map<List<DAOIndex>, List<DAOListDto>>(daoList);
        var symbols = items.Select(x => x.Symbol.ToUpper()).Distinct().ToList();
        var tokenInfos = new Dictionary<string, TokenInfoDto>();
        foreach (var symbol in symbols)
        {
            tokenInfos[symbol] = await _tokenService.GetTokenInfoAsync(chainId, symbol);
        }

        var daoIds = items.Select(s => s.DaoId).ToHashSet();
        var proposalCountDic = await _proposalProvider.GetProposalCountByDaoIds(chainId, daoIds);

        foreach (var dao in items)
        {
            if (!dao.Symbol.IsNullOrEmpty())
            {
                dao.SymbolHoldersNum = tokenInfos.TryGetValue(dao.Symbol.ToUpper(), out var tokenInfo)
                    ? long.Parse(tokenInfo.Holders)
                    : 0L;
            }

            if (!dao.IsNetworkDAO)
            {
                if (proposalCountDic.ContainsKey(dao.DaoId))
                {
                    dao.ProposalsNum = proposalCountDic[dao.DaoId];
                }
            }
            else
            {
                dao.HighCouncilMemberCount = (await _graphQlProvider.GetBPAsync(chainId)).Count;
                dao.ProposalsNum = await _graphQlProvider.GetProposalNumAsync(chainId);
            }
        }

        return new Tuple<long, List<DAOListDto>>(originResult.Item1, items);
    }

    public async Task<List<string>> GetBPList(string chainId)
    {
        return await _graphQlProvider.GetBPAsync(chainId);
    }

    public async Task<List<MyDAOListDto>> GetMyDAOListAsync(QueryMyDAOListInput input)
    {
        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        var result = new List<MyDAOListDto>();
        if (address.IsNullOrEmpty())
        {
            return result;
        }

        switch (input.Type)
        {
            case MyDAOType.All:
                var ownedTask = GetMyOwnedDaoListDto(input, address);
                var participatedTask = GetMyParticipatedDaoListDto(input, address);
                var managedTask = GetMyManagedDaoListDto(input, address);
                await Task.WhenAll(ownedTask, participatedTask, managedTask);
                result.Add(ownedTask.Result);
                result.Add(participatedTask.Result);
                result.Add(managedTask.Result);
                break;
            case MyDAOType.Owned:
                result.Add(await GetMyOwnedDaoListDto(input, address));
                break;
            case MyDAOType.Participated:
                result.Add(await GetMyParticipatedDaoListDto(input, address));
                break;
            case MyDAOType.Managed:
                result.Add(await GetMyManagedDaoListDto(input, address));
                break;
        }

        return result;
    }

    public async Task<bool> IsDaoMemberAsync(IsDaoMemberInput input)
    {
        try
        {
            var memberDto = await _daoProvider.GetMemberAsync(new GetMemberInput
            {
                ChainId = input.ChainId,
                DAOId = input.DAOId,
                Address = input.MemberAddress
            });
            return memberDto != null && !memberDto.Address.IsNullOrWhiteSpace();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "IsDaoMemberAsync error. input={0}", JsonConvert.SerializeObject(input));
            throw new UserFriendlyException($"Exception in checking if user is a DAO member, {e.Message}");
        }
    }

    private async Task<MyDAOListDto> GetMyManagedDaoListDto(QueryMyDAOListInput input, string address)
    {
        var bpList = await GetBPList(input.ChainId);

        var managedDaoIndices = await _electionProvider.GetHighCouncilManagedDaoIndexAsync(
            new GetHighCouncilMemberManagedDaoInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                SkipCount = 0,
                ChainId = input.ChainId,
                MemberAddress = address
            });
        var daoIds = managedDaoIndices.Select(item => item.DaoId).ToList();

        var (totalCount, daoList) = await _daoProvider.GetManagedDAOAsync(input, daoIds, bpList.Contains(address));
        return await GetMyDaoListDto(MyDAOType.Managed, input.ChainId,
            new Tuple<long, List<DAOIndex>>(totalCount, daoList));
    }

    private async Task<MyDAOListDto> GetMyOwnedDaoListDto(QueryMyDAOListInput input, string address)
    {
        var ownedResult = await _daoProvider.GetMyOwneredDAOListAsync(input, address);
        return await GetMyDaoListDto(MyDAOType.Owned, input.ChainId, ownedResult);
    }

    private async Task<MyDAOListDto> GetMyParticipatedDaoListDto(QueryMyDAOListInput input, string address)
    {
        var participatedResult = await _daoProvider.GetMyParticipatedDaoListAsync(new GetParticipatedInput
        {
            Address = address, ChainId = input.ChainId, SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        });
        if (participatedResult.Data.IsNullOrEmpty())
        {
            return new MyDAOListDto();
        }

        var daoIds = participatedResult.Data.Select(participated => participated.Id).ToList();
        var daoList = await _daoProvider.GetDaoListByDaoIds(input.ChainId, daoIds);
        //var daoList = _objectMapper.Map<List<IndexerDAOInfo>, List<DAOIndex>>(participatedResult.Data);
        return await GetMyDaoListDto(MyDAOType.Participated, input.ChainId,
            new Tuple<long, List<DAOIndex>>(participatedResult.TotalCount, daoList));
    }

    private async Task<MyDAOListDto> GetMyDaoListDto(MyDAOType type, string chainId,
        Tuple<long, List<DAOIndex>> originResult)
    {
        return new MyDAOListDto
        {
            Type = type, TotalCount = originResult.Item1, List = (await FillDaoListAsync(chainId, originResult)).Item2
        };
    }

    private Task<ExplorerProposalResponse> GetCountTask(Common.Enum.ProposalType type)
    {
        return _explorerProvider.GetProposalPagerAsync(CommonConstant.MainChainId, new ExplorerProposalListRequest
        {
            ProposalType = type.ToString(),
            Status = "all", IsContract = 0
        });
    }
}