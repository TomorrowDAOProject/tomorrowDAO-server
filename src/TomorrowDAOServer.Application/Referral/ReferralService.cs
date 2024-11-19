using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Linq;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Dto;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Referral;

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
    private readonly IReferralCycleProvider _referralCycleProvider;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;
    private readonly ITelegramUserInfoProvider _telegramUserInfoProvider;

    public ReferralService(IReferralInviteProvider referralInviteProvider, IUserProvider userProvider, 
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider, IUserAppService userAppService, 
        IOptionsMonitor<RankingOptions> rankingOptions, ILogger<IReferralService> logger, IPortkeyProvider portkeyProvider,
        IReferralCycleProvider referralCycleProvider, IOptionsMonitor<TelegramOptions> telegramOptions, 
        ITelegramUserInfoProvider telegramUserInfoProvider)
    {
        _referralInviteProvider = referralInviteProvider;
        _userProvider = userProvider;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _userAppService = userAppService;
        _rankingOptions = rankingOptions;
        _logger = logger;
        _portkeyProvider = portkeyProvider;
        _referralCycleProvider = referralCycleProvider;
        _telegramOptions = telegramOptions;
        _telegramUserInfoProvider = telegramUserInfoProvider;
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
        var (address, addressCaHash) = await _userProvider.GetAndValidateUserAddressAndCaHashAsync(CurrentUser.GetId(), chainId);
        var currentCycle = await _referralCycleProvider.GetCurrentCycleAsync();
        var accountCreation = 0L;
        var votigramVote = 0L;
        var votigramActivityVote = 0L;
        var estimatedReward = 0L;
        var startTime = 0L;
        var endTime = 0L;
        if (_rankingOptions.CurrentValue.ReferralActivityValid && IsCycleValid(currentCycle))
        {
            startTime = currentCycle.StartTime;
            endTime = currentCycle.EndTime;
            accountCreation = await _referralInviteProvider.GetAccountCreationAsync(startTime, endTime, chainId, addressCaHash);
            votigramVote = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(startTime, endTime, chainId, addressCaHash, true);
            votigramActivityVote = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(startTime, endTime, chainId, addressCaHash, true, true);
            estimatedReward = _rankingAppPointsCalcProvider.CalculatePointsFromReferralVotes(votigramActivityVote);
        }
        var accountCreationAll= await _referralInviteProvider.GetAccountCreationAsync(0, 0, chainId, addressCaHash);
        var votigramVoteAll = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(0, 0, chainId, addressCaHash, true);
        var votigramActivityVoteAll = await _referralInviteProvider.GetInvitedCountByInviterCaHashAsync(0, 0, chainId, addressCaHash, true, true);
        var estimatedRewardAll = _rankingAppPointsCalcProvider.CalculatePointsFromReferralVotes(votigramActivityVoteAll);
        return new InviteDetailDto
        {
            EstimatedReward = estimatedReward,
            AccountCreation = accountCreation,
            VotigramVote = votigramVote,
            VotigramActivityVote = votigramActivityVote,
            EstimatedRewardAll = estimatedRewardAll,
            AccountCreationAll = accountCreationAll,
            VotigramVoteAll = votigramVoteAll,
            VotigramActivityVoteAll = votigramActivityVoteAll,
            StartTime = startTime,
            EndTime = endTime,
            DuringCycle = true,
            Address = address,
            CaHash = addressCaHash
        };
    }

    public async Task<InviteBoardPageResultDto<InviteLeaderBoardDto>> InviteLeaderBoardAsync(InviteLeaderBoardInput input)
    {
        var (address, addressCaHash) = await _userProvider.GetAndValidateUserAddressAndCaHashAsync(CurrentUser.IsAuthenticated ? 
            CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (input.StartTime == 0 || input.EndTime == 0)
        {
            var (startTime, endTime) = await GetLeaderBoardTime();
            input.StartTime = startTime;
            input.EndTime = endTime;
        }
        var inviterBuckets = await _referralInviteProvider.InviteLeaderBoardAsync(input.StartTime, input.EndTime);
        var caHashList = inviterBuckets.Select(bucket => bucket.Key).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var userList = await _userAppService.GetUserByCaHashListAsync(caHashList);
        var inviterList = RankHelper.GetRankedList(input.ChainId, userList, inviterBuckets);
        var addressList = userList.Where(x => x.AddressInfos != null)
            .Where(x => x.AddressInfos.Any(ai => ai.ChainId == input.ChainId))
            .Select(x => x.AddressInfos.First().Address).Distinct().ToList();
        if (!addressList.Contains(address))
        {
            addressList.Add(address);
        }
        var me = inviterList.Find(x => x.InviterCaHash == addressCaHash);
        var tgInfoList = await _telegramUserInfoProvider.GetByAddressListAsync(addressList);
        var tgInfoDIc = tgInfoList.ToDictionary(x => x.Address, x => x);
        FillTgInfo(tgInfoDIc, me);
        foreach (var dto in inviterList)
        {
            FillTgInfo(tgInfoDIc, dto);
        }
        return new InviteBoardPageResultDto<InviteLeaderBoardDto>
        {
            TotalCount = inviterList.Count,
            Data = inviterList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList(),
            Me = me
        };
    }

    public async Task<ReferralActiveConfigDto> ConfigAsync()
    {
        var cycles = (await _referralCycleProvider.GetEffectCyclesAsync())
            .OrderByDescending(c => c.StartTime).ToList();
        return new ReferralActiveConfigDto
        {
            Config = cycles.Select(cycle => new ReferralActiveDto
            {
                StartTime = cycle.StartTime, EndTime = cycle.EndTime
            }).ToList()
        };
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

    public async Task SetCycleAsync(SetCycleInput input)
    {
        var chainId = input.ChainId;
        var startTime = input.StartTime;
        var endTime = input.EndTime;
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }
        
        await _referralCycleProvider.AddOrUpdateAsync(new ReferralCycleIndex
        {
            Id = GuidHelper.GenerateGrainId(chainId, startTime, endTime),
            ChainId = chainId, StartTime = startTime, EndTime = endTime, PointsDistribute = input.PointsDistribute
        });
    }

    private async Task<Tuple<long, long>> GetLeaderBoardTime()
    {
        var currentCycle = await _referralCycleProvider.GetCurrentCycleAsync();
        if (IsCycleValid(currentCycle))
        {
            return new Tuple<long, long>(currentCycle.StartTime, currentCycle.EndTime);
        }

        var latestCycle = await _referralCycleProvider.GetLatestCycleAsync();
        if (IsCycleValid(latestCycle))
        {
            return new Tuple<long, long>(latestCycle.StartTime, latestCycle.EndTime);
        }

        var config = _rankingOptions.CurrentValue.ParseReferralActiveTimes().Config.First();
        return new Tuple<long, long>(config.StartTime, config.EndTime);
    }

    private bool IsCycleValid(ReferralCycleIndex cycle)
    {
        return cycle != null && cycle.StartTime != 0 && cycle.EndTime != 0;
    }

    private void FillTgInfo(Dictionary<string, TelegramUserInfoIndex> infoDic, InviteLeaderBoardDto dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.Inviter))
        {
            return;
        }
        var address = dto.Inviter;
        if (infoDic.TryGetValue(address, out var info))
        {
            ObjectMapper.Map(info, dto);
        }
    }
}