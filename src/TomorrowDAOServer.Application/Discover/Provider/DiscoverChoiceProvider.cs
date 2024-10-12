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
    Task<bool> DiscoverViewedAsync(string chainId, string address);
    Task<bool> GetExistByAddressAndDiscoverTypeAsync(string chainId, string address, DiscoverChoiceType type);
    Task BulkAddOrUpdateAsync(List<DiscoverChoiceIndex> list);
    Task<List<DiscoverChoiceIndex>> GetByAddressAsync(string chainId, string address);
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

    public async Task<bool> DiscoverViewedAsync(string chainId, string address)
    {
        var grain = _clusterClient.GetGrain<IDiscoverViewedGrain>(GuidHelper.GenerateGrainId(chainId, address));
        return await grain.DiscoverViewedAsync();
    }

    public async Task<bool> GetExistByAddressAndDiscoverTypeAsync(string chainId, string address, DiscoverChoiceType type)
    {
        var query = new SearchDescriptor<DiscoverChoiceIndex>()
            .Query(q => q.Bool(b => b
                .Must(
                    q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
                    q => q.Term(i => i.Field(t => t.Address).Value(address)),
                    q => q.Term(i => i.Field(t => t.DiscoverChoiceType).Value(type))
                )
            ))
            .Size(0); 
        var response = await _userDiscoverChoiceRepository.SearchAsync(query, 0, int.MaxValue);
        return response.IsValid && response.Total > 0;
    }

    public async Task BulkAddOrUpdateAsync(List<DiscoverChoiceIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        
        await _userDiscoverChoiceRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<DiscoverChoiceIndex>> GetByAddressAsync(string chainId, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DiscoverChoiceIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.Address).Value(address))
        };
        QueryContainer Filter(QueryContainerDescriptor<DiscoverChoiceIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _userDiscoverChoiceRepository.GetListAsync(Filter)).Item2;
    }
}