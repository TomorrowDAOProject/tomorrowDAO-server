using System.Collections.Generic;

namespace TomorrowDAOServer.Referral.Dto;

public class ReferralActiveConfigDto
{
    public List<ReferralActiveDto> Config { get; set; }
}

public class ReferralActiveDto
{
    public long StartTime { get; set; }
    public long EndTime { get; set; }
}