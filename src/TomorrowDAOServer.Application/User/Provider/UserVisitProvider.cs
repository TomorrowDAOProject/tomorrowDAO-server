using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserVisitProvider
{
    Task AddOrUpdateAsync(UserVisitIndex index);
}

public class UserVisitProvider : IUserVisitProvider, ISingletonDependency
{
    private readonly INESTRepository<UserVisitIndex, string> _userVisitRepository;

    public UserVisitProvider(INESTRepository<UserVisitIndex, string> userVisitRepository)
    {
        _userVisitRepository = userVisitRepository;
    }

    public async Task AddOrUpdateAsync(UserVisitIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userVisitRepository.AddOrUpdateAsync(index);
    }
}