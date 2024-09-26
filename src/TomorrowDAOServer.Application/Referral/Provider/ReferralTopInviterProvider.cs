using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Referral.Provider;

public interface IReferralTopInviterProvider
{
    Task BulkAddOrUpdateAsync(List<ReferralTopInviterIndex> list);
    Task<bool> GetExistByTimeAsync(long startTime, long endTime);
}

public class ReferralTopInviterProvider : IReferralTopInviterProvider, ISingletonDependency
{
    private readonly INESTRepository<ReferralTopInviterIndex, string> _referralTopInviterRepository;

    public ReferralTopInviterProvider(INESTRepository<ReferralTopInviterIndex, string> referralTopInviterRepository)
    {
        _referralTopInviterRepository = referralTopInviterRepository;
    }

    public async Task BulkAddOrUpdateAsync(List<ReferralTopInviterIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }

        await _referralTopInviterRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<bool> GetExistByTimeAsync(long startTime, long endTime)
    {
        var query = new SearchDescriptor<ReferralTopInviterIndex>()
            .Query(q => q.Bool(b => b
                .Must(
                    m => m.Term(t => t.Field(f => f.StartTime).Value(startTime)),
                    m => m.Term(t => t.Field(f => f.EndTime).Value(endTime))
                )
            ))
            .Size(0); 
        var response = await _referralTopInviterRepository.SearchAsync(query, 0, int.MaxValue);
        return response.IsValid && response.Total > 0;
    }
}