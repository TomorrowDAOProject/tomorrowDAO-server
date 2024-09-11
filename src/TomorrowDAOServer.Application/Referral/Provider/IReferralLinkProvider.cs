// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using AElf.Indexing.Elasticsearch;
// using Nest;
// using TomorrowDAOServer.Common;
// using TomorrowDAOServer.Entities;
// using Volo.Abp.DependencyInjection;
//
// namespace TomorrowDAOServer.Referral.Provider;
//
// public interface IReferralLinkProvider
// {
//     Task<ReferralLinkIndex> GetByInviterAsync(string chainId, string address);
//     Task GenerateLinkAsync(string chainId, string address, string link, string code);
//     Task<List<ReferralLinkIndex>> GetByReferralCodesAsync(string chainId, List<string> codes);
// }
//
// public class ReferralLinkProvider : IReferralLinkProvider, ISingletonDependency
// {
//     private readonly INESTRepository<ReferralLinkIndex, string> _referralLinkRepository;
//
//     public ReferralLinkProvider(INESTRepository<ReferralLinkIndex, string> referralLinkRepository)
//     {
//         _referralLinkRepository = referralLinkRepository;
//     }
//
//     public async Task<ReferralLinkIndex> GetByInviterAsync(string chainId, string address)
//     {
//         var mustQuery = new List<Func<QueryContainerDescriptor<ReferralLinkIndex>, QueryContainer>>
//         {
//             q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
//             q => q.Term(i => i.Field(t => t.Inviter).Value(address))
//         };
//         QueryContainer Filter(QueryContainerDescriptor<ReferralLinkIndex> f) => f.Bool(b => b.Must(mustQuery));
//         return await _referralLinkRepository.GetAsync(Filter);
//     }
//
//     public async Task GenerateLinkAsync(string chainId, string address, string link, string code)
//     {
//         if (code.IsNullOrEmpty())
//         { 
//             return;   
//         }
//         await _referralLinkRepository.AddOrUpdateAsync(new ReferralLinkIndex
//         {
//             Id = GuidHelper.GenerateId(chainId, address),
//             ChainId = chainId,
//             Inviter = address,
//             ReferralLink = link,
//             ReferralCode = code
//         });
//     }
//
//     public async Task<List<ReferralLinkIndex>> GetByReferralCodesAsync(string chainId, List<string> codes)
//     {
//         var mustQuery = new List<Func<QueryContainerDescriptor<ReferralLinkIndex>, QueryContainer>>
//         {
//             q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
//             q => q.Terms(i => i.Field(t => t.ReferralCode).Terms(codes))
//         };
//         QueryContainer Filter(QueryContainerDescriptor<ReferralLinkIndex> f) => f.Bool(b => b.Must(mustQuery));
//         return (await _referralLinkRepository.GetListAsync(Filter)).Item2;
//     }
// }