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
    Task<ReferralInviteIndex> GetByNotVoteInviteeAsync(string chainId, string invitee);
    Task<List<ReferralInviteIndex>> GetByIdsAsync(List<string> ids);
    Task BulkAddOrUpdateAsync(List<ReferralInviteIndex> list);
    Task<List<ReferralInviteIndex>> GetByNotVoteInvitees(string chainId, List<string> invitees);
    Task AddOrUpdateAsync(ReferralInviteIndex index);
    Task<long> GetInvitedCountByInviterAsync(string chainId, string inviter, bool isVoted);
    Task<IReadOnlyCollection<KeyedBucket<string>>> InviteLeaderBoardAsync(InviteLeaderBoardInput input);
}

public class ReferralInviteProvider : IReferralInviteProvider, ISingletonDependency
{
    private readonly INESTRepository<ReferralInviteIndex, string> _referralInviteRepository;

    public ReferralInviteProvider(INESTRepository<ReferralInviteIndex, string> referralInviteRepository)
    {
        _referralInviteRepository = referralInviteRepository;
    }

    public async Task<ReferralInviteIndex> GetByNotVoteInviteeAsync(string chainId, string invitee)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.Invitee).Value(invitee))
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<ReferralInviteIndex>, QueryContainer>>
        {
            q => q.Exists(e => e.Field(f => f.FirstVoteTime))
        };

        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteIndex> f) => f.Bool(b => b
            .Must(mustQuery).MustNot(mustNotQuery));
        return await _referralInviteRepository.GetAsync(Filter);
    }

    public async Task<List<ReferralInviteIndex>> GetByIdsAsync(List<string> ids)
    {
        if (ids.IsNullOrEmpty())
        {
            return new List<ReferralInviteIndex>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(t => t.Id).Terms(ids))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _referralInviteRepository.GetListAsync(Filter)).Item2;
    }

    public async Task BulkAddOrUpdateAsync(List<ReferralInviteIndex> list)
    {
        await _referralInviteRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<ReferralInviteIndex>> GetByNotVoteInvitees(string chainId, List<string> invitees)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),  
            q => q.Terms(i => i.Field(t => t.Invitee).Terms(invitees))
        };

        var mustNotQuery = new List<Func<QueryContainerDescriptor<ReferralInviteIndex>, QueryContainer>>
        {
            q => q.Exists(e => e.Field(f => f.FirstVoteTime))
        };

        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteIndex> f) => f.Bool(b => b
                .Must(mustQuery).MustNot(mustNotQuery));

        return (await _referralInviteRepository.GetListAsync(Filter)).Item2;
    }


    public async Task AddOrUpdateAsync(ReferralInviteIndex index)
    {
        await _referralInviteRepository.AddOrUpdateAsync(index);
    }

    public async Task<long> GetInvitedCountByInviterAsync(string chainId, string inviter, bool isVoted)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralInviteIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.Inviter).Value(inviter))
        };
        if (isVoted)
        {
            // FirstVoteTime not null
            mustQuery.Add(q => q.Exists(e => e.Field(f => f.FirstVoteTime)));
        }
        QueryContainer Filter(QueryContainerDescriptor<ReferralInviteIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _referralInviteRepository.CountAsync(Filter)).Count;
    }

    public async Task<IReadOnlyCollection<KeyedBucket<string>>> InviteLeaderBoardAsync(InviteLeaderBoardInput input)
    {
        var query = new SearchDescriptor<ReferralInviteIndex>()
            .Query(q => q.Exists(e => e.Field(f => f.FirstVoteTime)))  
            .Query(q => q.DateRange(r => r
                .Field(f => f.FirstVoteTime)
                .GreaterThanOrEquals(input.StartTime)
                .LessThanOrEquals(input.EndTime)))  
            .Aggregations(a => a
                .Terms("inviter_agg", t => t
                    .Field(f => f.Inviter)
                    .Size(int.MaxValue)  
                    .Order(o => o
                        .Descending("invite_count"))  
                    .Aggregations(aa => aa.ValueCount("invite_count", vc => vc
                        .Field(f => f.Id)))));
        var response = await _referralInviteRepository.SearchAsync(query, 0, int.MaxValue);
        return response.Aggregations.Terms("inviter_agg").Buckets;
    }
}