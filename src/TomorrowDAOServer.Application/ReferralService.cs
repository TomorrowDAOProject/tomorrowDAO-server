using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral;
using TomorrowDAOServer.Referral.Dto;
using TomorrowDAOServer.Referral.Provider;
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
    private readonly IReferralLinkProvider _referralLinkProvider;
    private readonly IUserProvider _userProvider;
    private readonly IPortkeyProvider _portkeyProvider;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;

    public ReferralService(IReferralInviteProvider referralInviteProvider, IReferralLinkProvider referralLinkProvider,
        IUserProvider userProvider, IPortkeyProvider portkeyProvider, IRankingAppPointsCalcProvider rankingAppPointsCalcProvider)
    {
        _referralInviteProvider = referralInviteProvider;
        _referralLinkProvider = referralLinkProvider;
        _userProvider = userProvider;
        _portkeyProvider = portkeyProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
    }

    public async Task<GetLinkDto> GetLinkAsync(string token, string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), chainId);
        var referralLink = await _referralLinkProvider.GetByInviterAsync(chainId, address);
        if (referralLink != null)
        {
            return new GetLinkDto { ReferralLink = referralLink.ReferralLink, ReferralCode = referralLink.ReferralCode };
        }

        var (link, code) = await _portkeyProvider.GetShortLingAsync(chainId, token);
        await _referralLinkProvider.GenerateLinkAsync(chainId, address, link, code);
        return new GetLinkDto { ReferralLink = link, ReferralCode = code};
    }

    public async Task<InviteDetailDto> InviteDetailAsync(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), chainId);
        var accountCreation = await _referralInviteProvider.GetInvitedCountByInviterAsync(chainId, address, false);
        var votigramVote = await _referralInviteProvider.GetInvitedCountByInviterAsync(chainId, address, true);
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
                Inviter = bucket.Key,
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