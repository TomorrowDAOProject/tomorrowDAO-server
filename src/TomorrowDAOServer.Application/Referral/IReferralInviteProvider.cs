using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Referral;

public interface IReferralInviteProvider
{
}

public class ReferralInviteProvider : IReferralInviteProvider, ISingletonDependency
{
    private readonly INESTRepository<ReferralInviteIndex, string> _referralInviteRepository;

    public ReferralInviteProvider(INESTRepository<ReferralInviteIndex, string> referralInviteRepository)
    {
        _referralInviteRepository = referralInviteRepository;
    }
}