using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserViewNewAppProvider
{
    Task AddOrUpdateAsync(UserViewNewAppIndex index);
    Task<UserViewNewAppIndex> GetByAddressAndTime(string address);
}

public class UserViewNewAppProvider : IUserViewNewAppProvider, ISingletonDependency
{
    private readonly INESTRepository<UserViewNewAppIndex, string> _userViewNewAppRepository;

    public UserViewNewAppProvider(INESTRepository<UserViewNewAppIndex, string> userViewNewAppRepository)
    {
        _userViewNewAppRepository = userViewNewAppRepository;
    }

    public async Task AddOrUpdateAsync(UserViewNewAppIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userViewNewAppRepository.AddOrUpdateAsync(index);
    }

    public async Task<UserViewNewAppIndex> GetByAddressAndTime(string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserViewNewAppIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Address).Value(address)),
        };
        QueryContainer Filter(QueryContainerDescriptor<UserViewNewAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userViewNewAppRepository.GetAsync(Filter);
    }
}