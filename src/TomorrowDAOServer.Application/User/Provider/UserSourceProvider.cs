using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserSourceProvider
{
    Task AddOrUpdateAsync(UserSourceIndex index);
}

public class UserSourceProvider : IUserSourceProvider, ISingletonDependency
{
    private readonly INESTRepository<UserSourceIndex, string> _userSourceRepository;

    public UserSourceProvider(INESTRepository<UserSourceIndex, string> userSourceRepository)
    {
        _userSourceRepository = userSourceRepository;
    }

    public async Task AddOrUpdateAsync(UserSourceIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userSourceRepository.AddOrUpdateAsync(index);
    }
}