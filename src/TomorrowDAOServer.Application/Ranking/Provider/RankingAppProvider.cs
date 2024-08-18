using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider;

public interface IRankingAppProvider
{
    Task BulkAddOrUpdateAsync(List<RankingAppIndex> list);
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
}