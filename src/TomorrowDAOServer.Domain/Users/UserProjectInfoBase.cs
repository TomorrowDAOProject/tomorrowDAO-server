using System;
using TomorrowDAOServer.Entities;
using Nest;

namespace TomorrowDAOServer.Users;

public class UserProjectInfoBase : AbstractEntity<string>
{
    [Keyword]
    public string ChainId { get; set; }
    
    public long BlockHeight { get; set; }

    [Keyword] public string User { get; set; }
    public long InvestAmount { get; set; }
    public long ToClaimAmount { get; set; }
    public long ActualClaimAmount { get; set; }
    public long LiquidatedDamageAmount { get; set; }
    public bool ClaimedLiquidatedDamage { get; set; }
    public DateTime? ClaimedLiquidatedDamageTime { get; set; }
    public DateTime CreateTime { get; set; }
}