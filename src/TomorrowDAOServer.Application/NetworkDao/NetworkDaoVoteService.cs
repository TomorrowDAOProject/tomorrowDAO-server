using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoVoteService : TomorrowDAOServerAppService, INetworkDaoVoteService
{
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly IObjectMapper _objectMapper;

    public NetworkDaoVoteService(INetworkDaoEsDataProvider networkDaoEsDataProvider, IObjectMapper objectMapper)
    {
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _objectMapper = objectMapper;
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
            resultDtos = _objectMapper.Map<List<NetworkDaoProposalVoteIndex>, List<GetAllPersonalVotesResultDto>>(voteIndices);
        }
        return new GetAllPersonalVotesPagedResult
        {
            Items = resultDtos,
            TotalCount = totalCount
        };

    }
}