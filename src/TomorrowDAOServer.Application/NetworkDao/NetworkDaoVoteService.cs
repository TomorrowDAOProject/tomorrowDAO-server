using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aetherlink.PriceServer.Common;
using Orleans;
using TomorrowDAOServer.Grains.Grain.NetworkDao;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.GrainDtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
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

    public NetworkDaoVoteService(INetworkDaoEsDataProvider networkDaoEsDataProvider, IObjectMapper objectMapper,
        IClusterClient clusterClient, IUserProvider userProvider, IExplorerProvider explorerProvider)
    {
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _userProvider = userProvider;
        _explorerProvider = explorerProvider;
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

        var resultDtos = _objectMapper.Map<List<NetworkDaoProposalVoteIndex>, List<GetVotedListResultDto>>(voteIndices);
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

    public async Task<AddTeamDescResultDto> AddTeamDescriptionAsync(AddTeamDescInput input)
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
        await _networkDaoEsDataProvider.BulkAddOrUpdateVoteTeamAsync(new List<NetworkDaoVoteTeamIndex>(){voteTeamIndex});

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

    public async Task<bool> LoadVoteTeamHistoryDateAsync(LoadVoteTeamDescHistoryInput input)
    {
        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (address.IsNullOrEmpty())
        {
            return false;
        }
        
        var allTeamDesc = await _explorerProvider.GetAllTeamDescAsync(input.ChainId, false);
        foreach (var networkDaoVoteTeamIndex in allTeamDesc)
        {
            var teamDescInput = _objectMapper.Map<NetworkDaoVoteTeamIndex, AddTeamDescInput>(networkDaoVoteTeamIndex);
            await AddTeamDescriptionAsync(teamDescInput);
        }
        
        allTeamDesc = await _explorerProvider.GetAllTeamDescAsync(input.ChainId, true);
        foreach (var networkDaoVoteTeamIndex in allTeamDesc)
        {
            var teamDescInput = _objectMapper.Map<NetworkDaoVoteTeamIndex, AddTeamDescInput>(networkDaoVoteTeamIndex);
            await AddTeamDescriptionAsync(teamDescInput);
        }

        return true;
    }
}