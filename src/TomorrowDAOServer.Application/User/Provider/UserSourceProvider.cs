using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserSourceProvider
{
    Task AddOrUpdateAsync(UserVisitSourceIndex index);
}

public class UserSourceProvider : IUserSourceProvider, ISingletonDependency
{
    private readonly INESTRepository<UserVisitSourceIndex, string> _userSourceRepository;

    public UserSourceProvider(INESTRepository<UserVisitSourceIndex, string> userSourceRepository)
    {
        _userSourceRepository = userSourceRepository;
    }

    public async Task AddOrUpdateAsync(UserVisitSourceIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userSourceRepository.AddOrUpdateAsync(index);
    }
}