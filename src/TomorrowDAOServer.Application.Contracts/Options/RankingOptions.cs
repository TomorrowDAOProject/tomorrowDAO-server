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
    public long PointsFirstReferralVote { get; set; } = 50000;
    public List<string> AllReferralActiveTime { get; set; } = new();
    public string ReferralDomain { get; set; }

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
    
    public TimeSpan GetLockUserTimeoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(LockUserTimeout);
    }

    public TimeSpan GetVoteTimoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(VoteTimeout);
    }
}