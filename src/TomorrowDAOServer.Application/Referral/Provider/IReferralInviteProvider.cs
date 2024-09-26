using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Grains.Grain.Referral;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Referral.Provider;

public interface IReferralInviteProvider
{
    Task<ReferralInviteRelationIndex> GetByNotVoteInviteeCaHashAsync(string chainId, string inviteeCaHash);
    Task<ReferralInviteRelationIndex> GetByInviteeCaHashAsync(string chainId, string inviteeCaHash);
    Task<List<ReferralInviteRelationIndex>> GetByIdsAsync(List<string> ids);
    Task BulkAddOrUpdateAsync(List<ReferralInviteRelationIndex> list);
    Task AddOrUpdateAsync(ReferralInviteRelationIndex relationIndex);
    Task<long> GetInvitedCountByInviterCaHashAsync(string chainId, string inviterCaHash, bool isVoted, bool isActivityVote = false);
    Task<IReadOnlyCollection<KeyedBucket<string>>> InviteLeaderBoardAsync(long startTime, long endTime);
    Task<List<ReferralInviteRelationIndex>> GetByTimeRangeAsync(long startTime, long endTime);
    Task<long> IncrementInviteCountAsync(string chainId, string address, long delta);
    Task<long> GetInviteCountAsync(string chainId, string address);
}

public class ReferralInviteProvider : IReferralInviteProvider, ISingletonDependency
{
    private readonly INESTRepository<ReferralInviteRelationIndex, string> _referralInviteRepository;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ReferralInviteProvider> _logger;

    public ReferralInviteProvider(INESTRepository<ReferralInviteRelationIndex, string> referralInviteRepository, IClusterClient clusterClient, ILogger<ReferralInviteProvider> logger)
    {
        _referralInviteRepository = referralInviteRepository;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<ReferralInviteRelationIndex> GetByNotVoteInviteeCaHashAsync(string chainId, string inviteeCaHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.InviteeCaHash).Value(inviteeCaHash))
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Exists(e => e.Field(f => f.FirstVoteTime))
        };

        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteRelationIndex> f) => f.Bool(b => b
            .Must(mustQuery).MustNot(mustNotQuery));
        return await _referralInviteRepository.GetAsync(Filter);
    }

    public async Task<ReferralInviteRelationIndex> GetByInviteeCaHashAsync(string chainId, string inviteeCaHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.InviteeCaHash).Value(inviteeCaHash))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteRelationIndex> f) => f.Bool(b => b
            .Must(mustQuery));
        return await _referralInviteRepository.GetAsync(Filter);
    }

    public async Task<List<ReferralInviteRelationIndex>> GetByIdsAsync(List<string> ids)
    {
        if (ids.IsNullOrEmpty())
        {
            return new List<ReferralInviteRelationIndex>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(t => t.Id).Terms(ids))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteRelationIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _referralInviteRepository.GetListAsync(Filter)).Item2;
    }

    public async Task BulkAddOrUpdateAsync(List<ReferralInviteRelationIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        await _referralInviteRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task AddOrUpdateAsync(ReferralInviteRelationIndex relationIndex)
    {
        if (relationIndex == null)
        {
            return;
        }
        await _referralInviteRepository.AddOrUpdateAsync(relationIndex);
    }

    public async Task<long> GetInvitedCountByInviterCaHashAsync(string chainId, string inviterCaHash, bool isVoted, bool isActivityVote = false)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.InviterCaHash).Value(inviterCaHash))
        };
        if (isVoted)
        {
            mustQuery.Add(q => q.Exists(e => e.Field(f => f.FirstVoteTime)));
        }

        if (isActivityVote)
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.IsReferralActivity).Value(true)));

        }
        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteRelationIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _referralInviteRepository.CountAsync(Filter)).Count;
    }

    public async Task<IReadOnlyCollection<KeyedBucket<string>>> InviteLeaderBoardAsync(long startTime, long endTime)
    {
        var query = new SearchDescriptor<ReferralInviteRelationIndex>()
            .Query(q => q.Bool(b => b
                .Must(
                    m => m.Exists(e => e.Field(f => f.FirstVoteTime))
                )
                .Filter(
                    f => f.Term(t => t.Field(f => f.IsReferralActivity).Value(true)),
                    f => f.Wildcard(w => w.Field(f => f.ReferralCode).Value("*?*")),
                    f => f.Wildcard(w => w.Field(f => f.InviterCaHash).Value("*?*"))
                )));

        if (startTime != 0 && endTime != 0)
        {
            DateTime starTimeDate = DateTimeOffset.FromUnixTimeMilliseconds(startTime).DateTime;
            DateTime endTimeDate = DateTimeOffset.FromUnixTimeMilliseconds(endTime).DateTime;
            query = query.Query(q => q.DateRange(r => r
                .Field(f => f.FirstVoteTime)
                .GreaterThanOrEquals(starTimeDate)
                .LessThanOrEquals(endTimeDate)));
        }

        query = query.Aggregations(a => a
            .Terms("inviter_agg", t => t
                .Field(f => f.InviterCaHash)
                .Size(int.MaxValue)
                .Order(o => o
                    .Descending("invite_count"))
                .Aggregations(aa => aa.ValueCount("invite_count", vc => vc
                    .Field(f => f.Id)))));

        var response = await _referralInviteRepository.SearchAsync(query, 0, int.MaxValue);
        return response.Aggregations.Terms("inviter_agg").Buckets;
    }

    public async Task<List<ReferralInviteRelationIndex>> GetByTimeRangeAsync(long startTime, long endTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Range(r => r  
                .Field(f => f.Timestamp)
                .GreaterThanOrEquals(startTime)
                .LessThanOrEquals(endTime))
        };

        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteRelationIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await IndexHelper.GetAllIndex(Filter, _referralInviteRepository);
    }

    public async Task<long> IncrementInviteCountAsync(string chainId, string address, long delta)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IReferralInviteCountGrain>(GuidHelper.GenerateGrainId(chainId, address));
            return await grain.IncrementInviteCountAsync(delta);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "IncrementInviteCountAsyncException chainId {chainId} address {address} delta {delta}", 
                chainId, address, delta);
            return -1;
        }
    }

    public async Task<long> GetInviteCountAsync(string chainId, string address)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IReferralInviteCountGrain>(GuidHelper.GenerateGrainId(chainId, address));
            return await grain.GetInviteCountAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetInviteCountAsyncException chainId {chainId} address {address}", chainId, address);
            return 0;
        }
    }
}