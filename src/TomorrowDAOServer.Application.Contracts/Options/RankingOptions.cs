using System;
using System.Collections.Generic;
using System.Linq;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Referral.Dto;

namespace TomorrowDAOServer.Options;

public class RankingOptions
{
    public List<string> DaoIds { get; set; } = new();
    public string DescriptionPattern { get; set; } = string.Empty;
    public string DescriptionBegin { get; set; } = string.Empty;
    //millisecond
    public long LockUserTimeout { get; set; } = 60000;
    //millisecond
    public long VoteTimeout { get; set; } = 60000;
    public int RetryTimes { get; set; } = 30;
    public int RetryDelay { get; set; } = 2000;
    public long PointsPerVote { get; set; } = 10000;
    public long PointsPerLike { get; set; } = 1;
    public long PointsFirstReferralVote { get; set; } = 5_0000;
    public long PointsReferralTopInviter { get; set; } = 10_0000;
    public long PointsDailyViewAsset { get; set; } = 1_0000;
    public long PointsDailyFirstInvite { get; set; } = 2_0000;
    public long PointsExploreJoinTgChannel { get; set; } = 1_0000;
    public long PointsExploreFollowX { get; set; } = 1_0000;
    public long PointsExploreJoinDiscord { get; set; } = 1_0000;
    public long PointsExploreCumulateFiveInvite { get; set; } = 10_0000;
    public long PointsExploreCumulateTenInvite { get; set; } = 30_0000;
    public long PointsExploreCumulateTwentyInvite { get; set; } = 50_0000;
    
    public List<string> AllReferralActiveTime { get; set; } = new();
    public string ReferralDomain { get; set; }
    public List<string> ReferralPointsAddressList { get; set; } = new();

    public ReferralActiveConfigDto ParseReferralActiveTimes()
    {
        var configDto = new ReferralActiveConfigDto
        {
            Config = new List<ReferralActiveDto>()
        };

        foreach (var timeParts in AllReferralActiveTime
                     .Select(timeString => timeString.Split(CommonConstant.Comma))
                     .Where(timeParts => timeParts.Length == 2))
        {
            configDto.Config.Add(new ReferralActiveDto
            {
                StartTime = long.Parse(timeParts[0]),
                EndTime = long.Parse(timeParts[1])
            });
        }

        configDto.Config = configDto.Config
            .OrderByDescending(c => c.StartTime)
            .ToList();

        return configDto;
    }

    public bool IsReferralActive()
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var config = ParseReferralActiveTimes();
        var latest = config.Config.FirstOrDefault();
        if (latest != null)
        {
            return currentTime >= latest.StartTime && currentTime <= latest.EndTime;
        }

        return false;
    }
    
    public Tuple<bool, ReferralActiveDto> IsLatestReferralActiveEnd()
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var latest = ParseReferralActiveTimes().Config.FirstOrDefault();
        if (latest != null)
        {
            return new Tuple<bool, ReferralActiveDto>(currentTime > latest.EndTime, latest) ;
        }

        return new Tuple<bool,ReferralActiveDto>(false, null);
    }

    public TimeSpan GetLockUserTimeoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(LockUserTimeout);
    }

    public TimeSpan GetVoteTimoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(VoteTimeout);
    }
}