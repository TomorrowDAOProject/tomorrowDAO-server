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
using TomorrowDAOServer.Providers;
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
    private readonly IPortkeyProvider _portkeyProvider;

    public ReferralService(IReferralInviteProvider referralInviteProvider, IUserProvider userProvider, 
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, IUserAppService userAppService, 
        IOptionsMonitor<RankingOptions> rankingOptions, ILogger<IReferralService> logger, IPortkeyProvider portkeyProvider)
    {
        _referralInviteProvider = referralInviteProvider;
        _userProvider = userProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _userAppService = userAppService;
        _rankingOptions = rankingOptions;
        _logger = logger;
        _portkeyProvider = portkeyProvider;
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
        var (latestReferralActiveEnd, latest) = _rankingOptions.CurrentValue.IsLatestReferralActiveEnd();
        if (latestReferralActiveEnd)
        {
            return new InviteDetailDto();
        }

        var startTime = latest.StartTime;
        var endTime = latest.EndTime;
        var accountCreation = await _referralInviteProvider.GetAccountCreationAsync(startTime, endTime, chainId, addressCaHash);
        var votigramVote = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(startTime, endTime, chainId, addressCaHash, true);
        var votigramActivityVote = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(startTime, endTime, chainId, addressCaHash, true, true);
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
        if (input.StartTime == 0 || input.EndTime == 0)
        {
            var latest = _rankingOptions.CurrentValue.ParseReferralActiveTimes().Config.First();
            input.StartTime = latest.StartTime;
            input.EndTime = latest.EndTime;
        }
        var inviterBuckets = await _referralInviteProvider.InviteLeaderBoardAsync(input.StartTime, input.EndTime);
        var caHashList = inviterBuckets.Select(bucket => bucket.Key).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var userList = await _userAppService.GetUserByCaHashListAsync(caHashList);
        var inviterList = RankHelper.GetRankedList(input.ChainId, userList, inviterBuckets);
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


        var userAddress = user.AddressInfos?.Find(a => a.ChainId == chainId)?.Address ?? string.Empty;
        var addressCaHash = user.CaHash;
        if (string.IsNullOrEmpty(userAddress))
        {
            throw new UserFriendlyException("No userAddress found");
        }

        var list = await _portkeyProvider.GetCaHolderTransactionAsync(chainId, userAddress);
        if (list == null || list.IsNullOrEmpty())
        {
            throw new UserFriendlyException("No userCaHolderInfo found");
        }
        
        var caHolder = list.First();
        var createTime = caHolder.Timestamp;
        if (DateTime.UtcNow.ToUtcSeconds() - createTime > 60)
        {
            _logger.LogInformation("ReferralBindingStatusAsyncOldUser address {0} caHash {1}", userAddress, addressCaHash);
            return new ReferralBindingStatusDto { NeedBinding = false, BindingSuccess = false };
        }
        
        var relation = await _referralInviteProvider.GetByInviteeCaHashAsync(chainId, addressCaHash);
        if (relation != null)
        {
            return relation.ReferralCode is CommonConstant.OrganicTraffic
                or CommonConstant.OrganicTrafficBeforeProjectCode
                ? new ReferralBindingStatusDto { NeedBinding = false, BindingSuccess = false }
                : new ReferralBindingStatusDto { NeedBinding = true, BindingSuccess = true };
        }

        _logger.LogInformation("ReferralBindingStatusAsyncNewUserWaitingBind address {0} caHash {1}", userAddress, addressCaHash);
        return new ReferralBindingStatusDto { NeedBinding = true, BindingSuccess = false };
    }
}