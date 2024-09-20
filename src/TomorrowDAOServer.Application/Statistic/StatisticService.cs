using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.Statistic.Dto;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace TomorrowDAOServer.Statistic;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class StatisticService : TomorrowDAOServerAppService, IStatisticService
{
    private readonly IReferralInviteProvider _referralInviteProvider;

    public StatisticService(IReferralInviteProvider referralInviteProvider)
    {
        _referralInviteProvider = referralInviteProvider;
    }

    public async Task<DauDto> GetDauAsync(GetDauInput input)
    {
        var dauReferral = await _referralInviteProvider.GetByTimeRangeAsync(input.StartTime, input.EndTime);
        return new DauDto
        {
            DauReferral = dauReferral
        };
    }
}