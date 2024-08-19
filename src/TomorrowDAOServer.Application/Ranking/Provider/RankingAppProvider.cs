using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider;

public interface IRankingAppProvider
{
    Task BulkAddOrUpdateAsync(List<RankingAppIndex> list);
    Task<List<RankingAppIndex>> GetByProposalIdAsync(string chainId, string proposalId);
}

public class RankingAppProvider : IRankingAppProvider, ISingletonDependency
{
    private readonly INESTRepository<RankingAppIndex, string> _rankingAppIndexRepository;

    public RankingAppProvider(INESTRepository<RankingAppIndex, string> rankingAppIndexRepository)
    {
        _rankingAppIndexRepository = rankingAppIndexRepository;
    }

    public async Task BulkAddOrUpdateAsync(List<RankingAppIndex> list)
    {
        await _rankingAppIndexRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<RankingAppIndex>> GetByProposalIdAsync(string chainId, string proposalId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppIndex>, QueryContainer>>
        {
            q => q.Terms(i =>
                i.Field(f => f.ChainId).Terms(chainId)), 
            q => q.Terms(i =>
                i.Field(f => f.ProposalId).Terms(proposalId))
            
        };
        QueryContainer Filter(QueryContainerDescriptor<RankingAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return (await _rankingAppIndexRepository.GetListAsync(Filter)).Item2;
    }
}