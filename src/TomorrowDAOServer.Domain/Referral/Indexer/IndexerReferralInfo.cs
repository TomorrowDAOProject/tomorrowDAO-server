using System.Collections.Generic;

namespace TomorrowDAOServer.Referral.Indexer;

public class IndexerReferralInfo 
{
    public ReferralInfoPage ReferralInfoPage { get; set; }
}

public class ReferralInfoPage
{
    public List<IndexerReferral> Data { get; set; }
}

public class IndexerReferral
{
    public string CaHash { get; set; }
    public string ReferralCode { get; set; }
    public string ProjectCode { get; set; }
    public string MethodName { get; set; }
    public long Timestamp { get; set; }
}