using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Referral.Provider;

public interface IReferralLinkProvider
{
    Task<ReferralLinkCodeIndex> GetByInviterAsync(string chainId, string caHash);
    Task BulkAddOrUpdateAsync(List<ReferralLinkCodeIndex> list);
    Task<List<ReferralLinkCodeIndex>> GetByReferralCodesAsync(string chainId, List<string> codes);
}

public class ReferralLinkProvider : IReferralLinkProvider, ISingletonDependency
{
    private readonly INESTRepository<ReferralLinkCodeIndex, string> _referralLinkRepository;

    public ReferralLinkProvider(INESTRepository<ReferralLinkCodeIndex, string> referralLinkRepository)
    {
        _referralLinkRepository = referralLinkRepository;
    }

    public async Task<ReferralLinkCodeIndex> GetByInviterAsync(string chainId, string caHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralLinkCodeIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.InviterCaHash).Value(caHash))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralLinkCodeIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _referralLinkRepository.GetAsync(Filter);
    }

    public async Task BulkAddOrUpdateAsync(List<ReferralLinkCodeIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        await _referralLinkRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<ReferralLinkCodeIndex>> GetByReferralCodesAsync(string chainId, List<string> codes)
    {
        if (codes.IsNullOrEmpty())
        {
            return new List<ReferralLinkCodeIndex>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralLinkCodeIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Terms(i => i.Field(t => t.ReferralCode).Terms(codes))
        };
        QueryContainer Filter(QueryContainerDescriptor<ReferralLinkCodeIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _referralLinkRepository.GetListAsync(Filter)).Item2;
    }
}