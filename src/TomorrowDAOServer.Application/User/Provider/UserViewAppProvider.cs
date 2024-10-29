using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserViewAppProvider
{
    Task AddOrUpdateAsync(UserViewAppIndex index);
    Task<UserViewAppIndex> GetByAddress(string address);
}

public class UserViewAppProvider : IUserViewAppProvider, ISingletonDependency
{
    private readonly INESTRepository<UserViewAppIndex, string> _userViewAppRepository;

    public UserViewAppProvider(INESTRepository<UserViewAppIndex, string> userViewAppRepository)
    {
        _userViewAppRepository = userViewAppRepository;
    }

    public async Task AddOrUpdateAsync(UserViewAppIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userViewAppRepository.AddOrUpdateAsync(index);
    }

    public async Task<UserViewAppIndex> GetByAddress(string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserViewAppIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Address).Value(address)),
        };
        QueryContainer Filter(QueryContainerDescriptor<UserViewAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userViewAppRepository.GetAsync(Filter);
    }
}