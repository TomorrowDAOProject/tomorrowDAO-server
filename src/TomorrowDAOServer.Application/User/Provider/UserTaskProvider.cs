using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserTaskProvider
{
    Task BulkAddOrUpdate(List<UserTaskIndex> list);
    Task AddOrUpdate(UserTaskIndex index);
}

public class UserTaskProvider : IUserTaskProvider, ISingletonDependency
{
    private readonly INESTRepository<UserTaskIndex, string> _userTaskRepository;

    public UserTaskProvider(INESTRepository<UserTaskIndex, string> userTaskRepository)
    {
        _userTaskRepository = userTaskRepository;
    }

    public async Task BulkAddOrUpdate(List<UserTaskIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }

        await _userTaskRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task AddOrUpdate(UserTaskIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userTaskRepository.AddOrUpdateAsync(index);
    }
}