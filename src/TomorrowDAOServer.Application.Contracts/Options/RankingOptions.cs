using System;
using System.Collections.Generic;
using System.Linq;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Referral.Dto;

namespace TomorrowDAOServer.Options;

public class RankingOptions
{
    public List<string> DaoIds { get; set; } = new();
    public List<string> CustomDaoIds { get; set; } = new();
    public string DescriptionPattern { get; set; } = string.Empty;
    public string DescriptionBegin { get; set; } = string.Empty;
    //millisecond
    public long LockUserTimeout { get; set; } = 60000;
    //millisecond
    public long VoteTimeout { get; set; } = 60000;
    public int RetryTimes { get; set; } = 30;
    public int RetryDelay { get; set; } = 2000;
    public long PointsPerVote { get; set; } = 200; //done
    public long PointsPerLike { get; set; } = 1;
    public long PointsFirstReferralVote { get; set; } = 1000; //done
    public long PointsReferralTopInviter { get; set; } = 1000;
    public long PointsDailyViewAsset { get; set; } = 100; //done
    public long PointsDailyFirstInvite { get; set; } = 1000; //done
    public long PointsExploreJoinTgChannel { get; set; } = 100; //done
    public long PointsExploreFollowX { get; set; } = 100; //done
    public long PointsExploreJoinDiscord { get; set; } = 100; //done
    public long PointsExploreCumulateFiveInvite { get; set; } = 2000; //done
    public long PointsExploreCumulateTenInvite { get; set; } = 4000; //done
    public long PointsExploreCumulateTwentyInvite { get; set; } = 1_0000; //done
    
    public long PointsExploreForwardX { get; set; } = 100; //done
    public long PointsViewAd { get; set; } = 100; //done
    public long PointsDailyCreatePoll { get; set; } = 1000; //done
    public long PointsExploreJoinVotigram{ get; set; } = 100; //done
    public long PointsExploreFollowVotigramX { get; set; } = 100; //done
    public long PointsExploreForwardVotigramX { get; set; } = 100; //done
    public List<long> PointsLogin { get; set; } = new();
    public long PointsExploreSchrodinger { get; set; } = 200; //done
    
    public List<string> AllReferralActiveTime { get; set; } = new();
    public string ReferralDomain { get; set; }
    public List<string> ReferralPointsAddressList { get; set; } = new();
    public bool RecordDiscover { get; set; } = false;
    public bool ReferralActivityValid { get; set; } = true;
    public bool IsWeeklyRankingsEnabled { get; set; } = false;
    public long GroupCount { get; set; } = 500;
    public List<string> TopRankingIds { get; set; } = new();
    public string TopRankingAddress { get; set; }
    public string TopRankingAccount { get; set; }
    public string TopRankingTitle { get; set; }
    public string TopRankingSchemeAddress { get; set; }
    public string TopRankingVoteSchemeId { get; set; }
    public string TopRankingBanner { get; set; }
    public DayOfWeek TopRankingGenerateTime { get; set; } = DayOfWeek.Sunday;
    public List<string> RankingExcludeIds { get; set; } = new();
    public List<string> AppNames { get; set; } = new();
    public long DailyMaxLikePoints { get; set; } = 1000;

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

    // public bool IsReferralActive(DateTime time)
    // {
    //     var utcMilliSeconds = time.ToUtcMilliSeconds();
    //     var config = ParseReferralActiveTimes();
    //     var latest = config.Config.FirstOrDefault();
    //     if (latest != null)
    //     {
    //         return utcMilliSeconds >= latest.StartTime && utcMilliSeconds <= latest.EndTime;
    //     }
    //
    //     return false;
    // }
    //
    // public Tuple<bool, ReferralActiveDto> IsLatestReferralActiveEnd()
    // {
    //     var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    //     var latest = ParseReferralActiveTimes().Config.FirstOrDefault();
    //     if (latest != null)
    //     {
    //         return new Tuple<bool, ReferralActiveDto>(currentTime > latest.EndTime, latest) ;
    //     }
    //
    //     return new Tuple<bool,ReferralActiveDto>(false, null);
    // }

    public TimeSpan GetLockUserTimeoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(LockUserTimeout);
    }

    public TimeSpan GetVoteTimoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(VoteTimeout);
    }
}