using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoVoteService : INetworkDaoVoteService, ISingletonDependency
{
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly IObjectMapper _objectMapper;

    public NetworkDaoVoteService(INetworkDaoEsDataProvider networkDaoEsDataProvider, IObjectMapper objectMapper)
    {
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _objectMapper = objectMapper;
    }

    public async Task<GetVotedListPageResult> GetVotedListAsync(GetVotedListInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
        {
            return new GetVotedListPageResult();
        }

        var (count, voteIndices) = await _networkDaoEsDataProvider.GetProposalVotedListAsync(input);
        if (voteIndices.IsNullOrEmpty())
        {
            return new GetVotedListPageResult
            {
                Items = new List<GetVotedListResultDto>(),
                TotalCount = count
            };
        }

        var resultDtos = _objectMapper.Map<List<NetworkDaoProposalVoteIndex>, List<GetVotedListResultDto>>(voteIndices);
        return new GetVotedListPageResult
        {
            Items = resultDtos,
            TotalCount = count
        };
    }
}