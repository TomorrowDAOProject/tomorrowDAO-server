using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Options;
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
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly ILogger<IReferralService> _logger;

    public ReferralService(IReferralInviteProvider referralInviteProvider, IUserProvider userProvider, 
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, IUserAppService userAppService, 
        IOptionsMonitor<RankingOptions> rankingOptions, ILogger<IReferralService> logger)
    {
        _referralInviteProvider = referralInviteProvider;
        _userProvider = userProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _userAppService = userAppService;
        _rankingOptions = rankingOptions;
        _logger = logger;
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
        var votigramActivityVote = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(chainId, addressCaHash, true);
        var estimatedReward = _rankingAppPointsCalcProvider.CalculatePointsFromReferralVotes(votigramActivityVote);
        return new InviteDetailDto
        {
            EstimatedReward = estimatedReward,
            AccountCreation = accountCreation,
            VotigramVote = votigramVote,
            VotigramActivityVote = votigramActivityVote
        };
    }

    public async Task<InviteBoardPageResultDto<InviteLeaderBoardDto>> InviteLeaderBoardAsync(InviteLeaderBoardInput input)
    {
        var (_, addressCaHash) = await _userProvider.GetAndValidateUserAddressAndCaHashAsync(CurrentUser.GetId(), input.ChainId);
        var inviterBuckets = await _referralInviteProvider.InviteLeaderBoardAsync(input);
        long rank = 1;           
        long lastInviteCount = -1;  
        long currentRank = 1;

        var caHashList = inviterBuckets.Select(bucket => bucket.Key).Distinct().ToList();
        _logger.LogInformation("InviteLeaderBoardAsync caHashList {0}", JsonConvert.SerializeObject(caHashList));
        var userList = await _userAppService.GetUserByCaHashListAsync(caHashList);
        var userDic = userList
            .Where(x => x.AddressInfos.Any(ai => ai.ChainId == input.ChainId))
            .GroupBy(ui => ui.CaHash)
            .ToDictionary(
                group => group.Key,
                group => group.First().AddressInfos.First(ai => ai.ChainId == input.ChainId)?.Address ?? string.Empty
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
                InviteAndVoteCount = inviteCount,
                Rank = currentRank  
            };
            rank++;  
            return referralInvite;
        }).ToList();
        var me = inviterList.Find(x => x.InviterCaHash == addressCaHash);
        return new InviteBoardPageResultDto<InviteLeaderBoardDto>
        {
            TotalCount = inviterList.Count,
            Data = inviterList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList(),
            Me = me
        };
    }

    public ReferralActiveConfigDto ConfigAsync()
    {
        return _rankingOptions.CurrentValue.ParseReferralActiveTimes();
    }

    public async Task<ReferralBindingStatusDto> ReferralBindingStatusAsync(string chainId)
    {
        var user = await _userProvider.GetUserAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty);
        if (user == null)
        {
            throw new UserFriendlyException("No user found");
        }

        var createTime = user.CreateTime;
        if (DateTime.UtcNow.ToUtcMilliSeconds() - createTime > 2 * 60 * 1000)
        {
            return new ReferralBindingStatusDto { NeedBinding = false, BindingSuccess = false };
        }

        var addressCaHash = user.CaHash;
        var relation = await _referralInviteProvider.GetByInviteeCaHashAsync(chainId, addressCaHash);
        if (relation == null)
        {
            return new ReferralBindingStatusDto { NeedBinding = true, BindingSuccess = false };
        }
        return relation.ReferralCode is CommonConstant.OrganicTraffic or CommonConstant.OrganicTrafficBeforeProjectCode 
            ? new ReferralBindingStatusDto { NeedBinding = false, BindingSuccess = false } 
            : new ReferralBindingStatusDto { NeedBinding = false, BindingSuccess = true };
    }
}