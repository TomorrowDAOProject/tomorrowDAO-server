using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral;
using TomorrowDAOServer.Referral.Dto;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace TomorrowDAOServer;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ReferralService : ApplicationService, IReferralService
{
    private readonly IReferralInviteProvider _referralInviteProvider;
    private readonly IUserProvider _userProvider;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;
    private readonly IUserAppService _userAppService;

    public ReferralService(IReferralInviteProvider referralInviteProvider, IUserProvider userProvider, 
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, IUserAppService userAppService)
    {
        _referralInviteProvider = referralInviteProvider;
        _userProvider = userProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _userAppService = userAppService;
    }

    // public async Task<GetLinkDto> GetLinkAsync(string token, string chainId)
    // {
    //     var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), chainId);
    //     var referralLink = await _referralLinkProvider.GetByInviterAsync(chainId, address);
    //     if (referralLink != null)
    //     {
    //         return new GetLinkDto { ReferralLink = referralLink.ReferralLink, ReferralCode = referralLink.ReferralCode };
    //     }
    //
    //     var (link, code) = await _portkeyProvider.GetShortLingAsync(chainId, token);
    //     await _referralLinkProvider.GenerateLinkAsync(chainId, address, link, code);
    //     return new GetLinkDto { ReferralLink = link, ReferralCode = code};
    // }

    public async Task<InviteDetailDto> InviteDetailAsync(string chainId)
    {
        var (_, addressCaHash) = await _userProvider.GetAndValidateUserAddressAndCaHashAsync(CurrentUser.GetId(), chainId);
        var accountCreation = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(chainId, addressCaHash, false);
        var votigramVote = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(chainId, addressCaHash, true);
        var estimatedReward = _rankingAppPointsCalcProvider.CalculatePointsFromReferralVotes(votigramVote);
        return new InviteDetailDto
        {
            EstimatedReward = estimatedReward,
            AccountCreation = accountCreation,
            VotigramVote = votigramVote
        };
    }

    public async Task<PageResultDto<InviteLeaderBoardDto>> InviteLeaderBoardAsync(InviteLeaderBoardInput input)
    {
        var inviterBuckets = await _referralInviteProvider.InviteLeaderBoardAsync(input);
        long rank = 1;           
        long lastInviteCount = -1;  
        long currentRank = 1;

        var caHashList = inviterBuckets.Select(bucket => bucket.Key).ToList();
        var userList = await _userAppService.GetUserByCaHashListAsync(caHashList);
        var userDic = userList
            .Where(x => x.AddressInfos.Any(ai => ai.ChainId == "a")) 
            .ToDictionary(
                ui => ui.CaHash, 
                ui => ui.AddressInfos.First(ai => ai.ChainId == "a").Address 
            );
        var inviterList = inviterBuckets.Select((bucket, _) =>
        {
            var inviteCount = (long)(bucket.ValueCount("invite_count").Value ?? 0);
            if (inviteCount != lastInviteCount)
            {
                currentRank = rank;
                lastInviteCount = inviteCount;
            }
            var referralInvite = new InviteLeaderBoardDto
            {
                InviterCaHash = bucket.Key,
                Inviter = userDic.GetValueOrDefault(bucket.Key, string.Empty),
                InviteCount = inviteCount,
                Rank = currentRank  
            };
            rank++;  
            return referralInvite;
        }).ToList();
        return new PageResultDto<InviteLeaderBoardDto>
        {
            TotalCount = inviterList.Count,
            Data = inviterList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList()
        };
    }
}