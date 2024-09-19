using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Referral.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Referral.Provider;

public interface IReferralInviteProvider
{
    Task<List<ReferralInviteRelationIndex>> GetByNotVoteAsync(string chainId, int skipCount);
    Task<ReferralInviteRelationIndex> GetByNotVoteInviteeCaHashAsync(string chainId, string inviteeCaHash);
    Task<ReferralInviteRelationIndex> GetByInviteeCaHashAsync(string chainId, string inviteeCaHash);
    Task<List<ReferralInviteRelationIndex>> GetByIdsAsync(List<string> ids);
    Task BulkAddOrUpdateAsync(List<ReferralInviteRelationIndex> list);
    Task AddOrUpdateAsync(ReferralInviteRelationIndex relationIndex);
    Task<long> GetInvitedCountByInviterCaHashAsync(string chainId, string inviterCaHash, bool isVoted, bool isActivityVote = false);
    Task<IReadOnlyCollection<KeyedBucket<string>>> InviteLeaderBoardAsync(InviteLeaderBoardInput input);
}

public class ReferralInviteProvider : IReferralInviteProvider, ISingletonDependency
{
    private readonly INESTRepository<ReferralInviteRelationIndex, string> _referralInviteRepository;

    public ReferralInviteProvider(INESTRepository<ReferralInviteRelationIndex, string> referralInviteRepository)
    {
        _referralInviteRepository = referralInviteRepository;
    }

    public async Task<List<ReferralInviteRelationIndex>> GetByNotVoteAsync(string chainId, int skipCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<ReferralInviteRelationIndex>, QueryContainer>>
        {
            q => q.Exists(e => e.Field(f => f.FirstVoteTime))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteRelationIndex> f) => f.Bool(b => b
            .Must(mustQuery).MustNot(mustNotQuery));

        var tuple = await _referralInviteRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending,
            sortExp: o => o.Timestamp, limit: 500);
        return tuple.Item2;
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

    public async Task<IReadOnlyCollection<KeyedBucket<string>>> InviteLeaderBoardAsync(InviteLeaderBoardInput input)
    {
        DateTime starTime = DateTimeOffset.FromUnixTimeMilliseconds(input.StartTime).DateTime;
        DateTime endTime = DateTimeOffset.FromUnixTimeMilliseconds(input.EndTime).DateTime;

        var query = new SearchDescriptor<ReferralInviteRelationIndex>()
            .Query(q => q.Bool(b => b
                .Must(
                    m => m.Exists(e => e.Field(f => f.FirstVoteTime)),  
                    m => m.Exists(e => e.Field(f => f.ReferralCode)),   
                    m => m.Exists(e => e.Field(f => f.InviterCaHash)),  
                    m => !m.Term(t => t.Field(f => f.ReferralCode).Value("")), 
                    m => !m.Term(t => t.Field(f => f.InviterCaHash).Value("")) 
                )
                .Filter(
                    f => f.Term(t => t.Field(f => f.IsReferralActivity).Value(true))  
                )));

        if (input.StartTime != 0 && input.EndTime != 0)
        {
            query = query.Query(q => q.DateRange(r => r
                .Field(f => f.FirstVoteTime)
                .GreaterThanOrEquals(starTime)
                .LessThanOrEquals(endTime)));
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
}