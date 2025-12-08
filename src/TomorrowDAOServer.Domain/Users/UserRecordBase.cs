using System;
using TomorrowDAOServer.Entities;
using Nest;

namespace TomorrowDAOServer.Users;

public class UserRecordBase : AbstractEntity<string>
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string User { get; set; }
    public BehaviorType BehaviorType { get; set; }
    public long ToRaiseTokenAmount { get; set; }
    public long CrowdFundingIssueAmount { get; set; }
    public DateTime DateTime { get; set; }
    public long BlockHeight { get; set; }
}