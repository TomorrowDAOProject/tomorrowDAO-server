// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;
// using TomorrowDAOServer.Chains;
// using TomorrowDAOServer.Common.Provider;
// using TomorrowDAOServer.Entities;
// using TomorrowDAOServer.Enums;
// using TomorrowDAOServer.Proposal.Provider;
// using TomorrowDAOServer.Referral.Provider;
// using TomorrowDAOServer.User;
// using TomorrowDAOServer.Vote.Provider;
//
// namespace TomorrowDAOServer.Referral;
//
// public class ReferralCheckDataService : ScheduleSyncDataService
// {
//     private readonly IChainAppService _chainAppService;
//     private readonly IReferralInviteProvider _referralInviteProvider;
//     private readonly ILogger<ScheduleSyncDataService> _logger;
//     private readonly IUserAppService _userAppService;
//     private readonly IVoteProvider _voteProvider;
//     private readonly IProposalProvider _proposalProvider;
//     private const int MaxResultCount = 1000;
//     
//     public ReferralCheckDataService(ILogger<ReferralCheckDataService> logger, 
//         IGraphQLProvider graphQlProvider, IChainAppService chainAppService, IReferralInviteProvider referralInviteProvider, 
//         IUserAppService userAppService, IVoteProvider voteProvider, IProposalProvider proposalProvider) : base(logger, graphQlProvider)
//     {
//         _logger = logger;
//         _chainAppService = chainAppService;
//         _referralInviteProvider = referralInviteProvider;
//         _userAppService = userAppService;
//         _voteProvider = voteProvider;
//         _proposalProvider = proposalProvider;
//     }
//
//     public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
//     {
//         var skipCount = 0;
//         List<ReferralInviteRelationIndex> queryList;
//         do
//         {
//             queryList = await _referralInviteProvider.GetByNotVoteAsync(chainId, skipCount);
//             if (queryList == null || queryList.IsNullOrEmpty())
//             {
//                 break;
//             }
//             _logger.LogInformation("NeedCheckReferralInviteRelationshipList skipCount {skipCount} count: {count}", skipCount, queryList?.Count);
//             var caHashList = queryList.Where(x => !string.IsNullOrEmpty(x.InviteeCaHash))
//                 .Select(x => x.InviteeCaHash).Distinct().ToList();
//             var userList = await _userAppService.GetUserByCaHashListAsync(caHashList);
//             var userAddressList = userList
//                 .Select(x => x.AddressInfos?.FirstOrDefault(x => x.ChainId == chainId)?.Address ?? string.Empty)
//                 .Where(address => !string.IsNullOrEmpty(address)) 
//                 .Distinct()
//                 .ToList();
//             var caHashToAddressDic = caHashList
//                 .Zip(userAddressList, (caHash, userAddress) => new { caHash, userAddress })
//                 .ToDictionary(x => x.caHash, x => x.userAddress);
//             var defaultProposal = await _proposalProvider.GetDefaultProposalAsync(chainId);
//             var voteRecords = await _voteProvider.GetByVotersAndVotingItemIdAsync(chainId, userAddressList, defaultProposal.ProposalId);
//             var voteDic = voteRecords.ToDictionary( x => x.Voter, x => x);
//             foreach (var invite in queryList)
//             {
//                 var inviteeCaHash = invite.InviteeCaHash;
//                 var inviteeAddress = caHashToAddressDic.GetValueOrDefault(inviteeCaHash, string.Empty);
//                 if (voteDic.TryGetValue(inviteeAddress, out var voteRecord))
//                 {
//                     invite.FirstVoteTime = voteRecord.VoteTime;
//                 }
//                 
//             }
//
//             skipCount += queryList.Count;
//         } while (!queryList.IsNullOrEmpty());
//         
//         
//         
//         throw new System.NotImplementedException();
//     }
//
//     public override async Task<List<string>> GetChainIdsAsync()
//     {
//         var chainIds = await _chainAppService.GetListAsync();
//         return chainIds.ToList();
//     }
//
//     public override WorkerBusinessType GetBusinessType()
//     {
//         return WorkerBusinessType.ReferralSync;
//     }
// }