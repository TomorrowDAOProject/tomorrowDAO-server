using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserPointsRecordProvider
{
    Task BulkAddOrUpdate(List<UserPointsRecordIndex> list);
    Task AddOrUpdate(UserPointsRecordIndex index);
}

public class UserPointsRecordProvider : IUserPointsRecordProvider, ISingletonDependency
{
    private readonly INESTRepository<UserPointsRecordIndex, string> _userPointsRecordRepository;

    public UserPointsRecordProvider(INESTRepository<UserPointsRecordIndex, string> userPointsRecordRepository)
    {
        _userPointsRecordRepository = userPointsRecordRepository;
    }

    public async Task BulkAddOrUpdate(List<UserPointsRecordIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }

        await _userPointsRecordRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task AddOrUpdate(UserPointsRecordIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userPointsRecordRepository.AddOrUpdateAsync(index);
    }
}