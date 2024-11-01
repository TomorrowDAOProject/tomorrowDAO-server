using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserViewAppProvider
{
    Task BulkAddOrUpdateAsync(List<UserViewedAppIndex> list);
    Task<UserViewedAppIndex> GetByAddress(string address);
    Task<List<UserViewedAppIndex>> GetByAliasList(List<string> aliases);
}

public class UserViewAppProvider : IUserViewAppProvider, ISingletonDependency
{
    private readonly INESTRepository<UserViewedAppIndex, string> _userViewAppRepository;

    public UserViewAppProvider(INESTRepository<UserViewedAppIndex, string> userViewAppRepository)
    {
        _userViewAppRepository = userViewAppRepository;
    }

    public async Task BulkAddOrUpdateAsync(List<UserViewedAppIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        await _userViewAppRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<UserViewedAppIndex> GetByAddress(string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserViewedAppIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Address).Value(address)),
        };
        QueryContainer Filter(QueryContainerDescriptor<UserViewedAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userViewAppRepository.GetAsync(Filter);
    }

    public async Task<List<UserViewedAppIndex>> GetByAliasList(List<string> aliases)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserViewedAppIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.Alias).Terms(aliases)),
        };
        QueryContainer Filter(QueryContainerDescriptor<UserViewedAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _userViewAppRepository);
    }
}