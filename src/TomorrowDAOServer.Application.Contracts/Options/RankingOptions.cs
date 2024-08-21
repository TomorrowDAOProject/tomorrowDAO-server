using System;
using System.Collections.Generic;
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

    public int RetryTimes { get; set; } = 20;

    public int RetryDelay { get; set; } = 2000;
    
    public TimeSpan GetLockUserTimeoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(LockUserTimeout);
    }

    public TimeSpan GetVoteTimoutTimeSpan()
    {
        return TimeSpan.FromMilliseconds(VoteTimeout);
    }
}