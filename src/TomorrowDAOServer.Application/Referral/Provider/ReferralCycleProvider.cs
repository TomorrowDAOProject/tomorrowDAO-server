using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Referral.Provider;

public interface IReferralCycleProvider
{
    Task AddOrUpdateAsync(ReferralCycleIndex cycleIndex);
    Task<List<ReferralCycleIndex>> GetEffectCyclesAsync();
    Task<List<ReferralCycleIndex>> GetEndAndNotDistributeCyclesAsync();
    Task<ReferralCycleIndex> GetCurrentCycleAsync();
    Task<ReferralCycleIndex> GetLatestCycleAsync();
}

public class ReferralCycleProvider : IReferralCycleProvider, ISingletonDependency
{
    private readonly ILogger<ReferralCycleProvider> _logger;
    private readonly INESTRepository<ReferralCycleIndex, string> _referralCycleRepository;

    public ReferralCycleProvider(ILogger<ReferralCycleProvider> logger,
        INESTRepository<ReferralCycleIndex, string> referralCycleRepository)
    {
        _logger = logger;
        _referralCycleRepository = referralCycleRepository;
    }

    public async Task AddOrUpdateAsync(ReferralCycleIndex cycleIndex)
    {
        await _referralCycleRepository.AddOrUpdateAsync(cycleIndex);
    }

    public async Task<List<ReferralCycleIndex>> GetEffectCyclesAsync()
    {
        var currentTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralCycleIndex>, QueryContainer>>
        {
            f => f.Range(
                r => r.Field(fld => fld.StartTime).LessThanOrEquals(currentTimeMillis))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralCycleIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _referralCycleRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<List<ReferralCycleIndex>> GetEndAndNotDistributeCyclesAsync()
    {
        var currentTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralCycleIndex>, QueryContainer>>()
        {
            q => q.Term(i => i.Field(f => f.PointsDistribute).Value(false)),
            f => f.Range(
                r => r.Field(fld => fld.EndTime).LessThanOrEquals(currentTimeMillis))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralCycleIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _referralCycleRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<ReferralCycleIndex> GetCurrentCycleAsync()
    {
        var currentTimeMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralCycleIndex>, QueryContainer>>
        {
            f => f.Range(
                r => r.Field(fld => fld.StartTime).LessThan(currentTimeMillis)),
            f => f.Range(
                r => r.Field(fld => fld.EndTime).GreaterThanOrEquals(currentTimeMillis))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralCycleIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _referralCycleRepository.GetAsync(Filter);
    }

    public async Task<ReferralCycleIndex> GetLatestCycleAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralCycleIndex>, QueryContainer>>();
        QueryContainer Filter(QueryContainerDescriptor<ReferralCycleIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (_, list) = await _referralCycleRepository.GetSortListAsync(Filter, skip: 0, limit: 1,
            sortFunc: _ => new SortDescriptor<ReferralCycleIndex>().Descending(index => index.EndTime));
        return list.IsNullOrEmpty() ? new ReferralCycleIndex() : list[0];
    }
}