using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Discover;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Discover.Provider;

public interface IDiscoverChoiceProvider
{
    Task<bool> DiscoverViewedAsync(string chainId, string address, string userId);
    Task<bool> GetExistByAddressAndUserIdAndDiscoverTypeAsync(string chainId, string userId, string address, DiscoverChoiceType type);
    Task BulkAddOrUpdateAsync(List<DiscoverChoiceIndex> list);
    Task<List<DiscoverChoiceIndex>> GetByAddressOrUserIdAsync(string chainId, string address, string userId);
}

public class DiscoverChoiceProvider : IDiscoverChoiceProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<DiscoverChoiceProvider> _logger;
    private readonly INESTRepository<DiscoverChoiceIndex, string> _userDiscoverChoiceRepository;

    public DiscoverChoiceProvider(IClusterClient clusterClient, ILogger<DiscoverChoiceProvider> logger, 
        INESTRepository<DiscoverChoiceIndex, string> userDiscoverChoiceRepository)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _userDiscoverChoiceRepository = userDiscoverChoiceRepository;
    }

    public async Task<bool> DiscoverViewedAsync(string chainId, string address, string userId)
    {
        var userIdGrain = _clusterClient.GetGrain<IDiscoverViewedGrain>(GuidHelper.GenerateGrainId(chainId, userId));
        var result = await userIdGrain.DiscoverViewedAsync();
        if (result || string.IsNullOrEmpty(address))
        {
            return result;
        }

        var addressGrain = _clusterClient.GetGrain<IDiscoverViewedGrain>(GuidHelper.GenerateGrainId(chainId, address));
        return await addressGrain.DiscoverViewedAsync();
    }

    public async Task<bool> GetExistByAddressAndUserIdAndDiscoverTypeAsync(string chainId, string userId, string address, DiscoverChoiceType type)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DiscoverChoiceIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.DiscoverChoiceType).Value(type))
        };

        var shouldQuery = new List<Func<QueryContainerDescriptor<DiscoverChoiceIndex>, QueryContainer>>();

        if (!string.IsNullOrEmpty(address))
        {
            shouldQuery.Add(q => q.Term(i => i.Field(t => t.Address).Value(address)));
        }
        shouldQuery.Add(q => q.Term(i => i.Field(t => t.UserId).Value(userId)));
        QueryContainer Filter(QueryContainerDescriptor<DiscoverChoiceIndex> f) => f.Bool(b => b
            .Must(mustQuery).Should(shouldQuery).MinimumShouldMatch(1));

        var count = (await _userDiscoverChoiceRepository.CountAsync(Filter)).Count;
        return count > 0;
    }

    public async Task BulkAddOrUpdateAsync(List<DiscoverChoiceIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        
        await _userDiscoverChoiceRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<DiscoverChoiceIndex>> GetByAddressOrUserIdAsync(string chainId, string address, string userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DiscoverChoiceIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId))
        };

        var shouldQuery = new List<Func<QueryContainerDescriptor<DiscoverChoiceIndex>, QueryContainer>>();
        if (!string.IsNullOrEmpty(address))
        {
            shouldQuery.Add(q => q.Term(i => i.Field(t => t.Address).Value(address)));
        }
        shouldQuery.Add(q => q.Term(i => i.Field(t => t.UserId).Value(userId)));
        QueryContainer Filter(QueryContainerDescriptor<DiscoverChoiceIndex> f) => f.Bool(b => b
            .Must(mustQuery).Should(shouldQuery).MinimumShouldMatch(1));

        return (await _userDiscoverChoiceRepository.GetListAsync(Filter)).Item2;
    }
}