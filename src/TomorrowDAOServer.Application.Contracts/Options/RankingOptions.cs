using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Utilities;

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
    public List<Tuple<long, long>> AllReferralActiveTime { get; set; } = new();
    
    public bool IsReferralActive()
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (AllReferralActiveTime == null || AllReferralActiveTime.Count == 0)
        {
            return false;
        }

        var latestInterval = AllReferralActiveTime.MaxBy(t => t.Item1);
        if (latestInterval == null)
        {
            return false;
        }

        var startTime = latestInterval.Item1; 
        var endTime = latestInterval.Item2;  

        return currentTime >= startTime && currentTime <= endTime;
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