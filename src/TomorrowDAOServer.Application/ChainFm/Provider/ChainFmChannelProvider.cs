using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.ChainFm.Index;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.ChainFm.Provider;

public interface IChainFmChannelProvider
{
    Task BulkAddOrUpdateAsync(List<ChainFmChannelIndex> channelIndices);

    Task<List<ChainFmChannelIndex>> GetTopFollowerChannelListAsync(int count);
}

public class ChainFmChannelProvider : IChainFmChannelProvider, ISingletonDependency
{
    private readonly ILogger<ChainFmChannelProvider> _logger;
    private readonly INESTRepository<ChainFmChannelIndex, string> _chainFmChannelIndexRepository;

    public ChainFmChannelProvider(ILogger<ChainFmChannelProvider> logger,
        INESTRepository<ChainFmChannelIndex, string> chainFmChannelIndexRepository)
    {
        _logger = logger;
        _chainFmChannelIndexRepository = chainFmChannelIndexRepository;
    }

    public async Task BulkAddOrUpdateAsync(List<ChainFmChannelIndex> channelIndices)
    {
        if (channelIndices.IsNullOrEmpty())
        {
            return;
        }

        await _chainFmChannelIndexRepository.BulkAddOrUpdateAsync(channelIndices);
    }

    public async Task<List<ChainFmChannelIndex>> GetTopFollowerChannelListAsync(int count)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ChainFmChannelIndex>, QueryContainer>>
        {
            q => q.Exists(e => e.Field(f => f.Follow_Count)),
            q => q.Range(r => r.Field(f => f.Follow_Count).GreaterThan(0)),
        };
        
        QueryContainer Filter(QueryContainerDescriptor<ChainFmChannelIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        IPromise<IList<ISort>> SortDescriptor(SortDescriptor<ChainFmChannelIndex> s) { return s.Descending(p => p.Follow_Count); }
        var (totalCount, list) = await _chainFmChannelIndexRepository.GetSortListAsync(Filter, skip: 0, limit: count, sortFunc: SortDescriptor);
        return list ?? new List<ChainFmChannelIndex>();
    }
}
