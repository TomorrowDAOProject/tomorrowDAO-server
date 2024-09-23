using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Referral.Provider;

public interface IReferralTopInviterProvider
{
    Task BulkAddOrUpdate(List<ReferralTopInviterIndex> list);
}

public class ReferralTopInviterProvider : IReferralTopInviterProvider, ISingletonDependency
{
    private readonly INESTRepository<ReferralTopInviterIndex, string> _referralTopInviterRepository;

    public ReferralTopInviterProvider(INESTRepository<ReferralTopInviterIndex, string> referralTopInviterRepository)
    {
        _referralTopInviterRepository = referralTopInviterRepository;
    }

    public async Task BulkAddOrUpdate(List<ReferralTopInviterIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }

        await _referralTopInviterRepository.BulkAddOrUpdateAsync(list);
    }
}