using System.Collections.Generic;

namespace TomorrowDAOServer.Referral.Dto;

public class ReferralCodeResponse
{
    public List<ReferralCodeInfo> Data { get; set; }
}

public class ReferralCodeInfo
{
    public string CaHash { get; set; }
    public string ReferralCode { get; set; }
}

public class ReferralCodeRequest
{
    public string ProjectCode { get; set; }
    public string ReferralCode { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}