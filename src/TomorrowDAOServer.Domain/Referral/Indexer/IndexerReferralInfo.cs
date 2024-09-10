using System.Collections.Generic;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Referral.Indexer;

public class IndexerReferralInfo : IndexerCommonResult<IndexerReferralInfo>
{
    public List<IndexerReferral> DataList { get; set; }
}

public class IndexerReferral
{
    public string CaHash { get; set; }
    public string ReferralCode { get; set; }
    public string ProjectCode { get; set; }
    public string MethodName { get; set; }
    public long Timestamp { get; set; }
}