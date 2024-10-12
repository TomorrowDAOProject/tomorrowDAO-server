using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserVisitSummaryProvider
{
    Task<UserVisitSummaryIndex> GetByIdAsync(string id);
    Task AddOrUpdateAsync(UserVisitSummaryIndex index);
}

public class UserVisitSummaryProvider : IUserVisitSummaryProvider, ISingletonDependency
{
    private readonly INESTRepository<UserVisitSummaryIndex, string> _userVisitSummaryRepository;

    public UserVisitSummaryProvider(INESTRepository<UserVisitSummaryIndex, string> userVisitSummaryRepository)
    {
        _userVisitSummaryRepository = userVisitSummaryRepository;
    }

    public async Task<UserVisitSummaryIndex> GetByIdAsync(string id)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserVisitSummaryIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.Id).Value(id))
        };
        QueryContainer Filter(QueryContainerDescriptor<UserVisitSummaryIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _userVisitSummaryRepository.GetAsync(Filter);
    }

    public async Task AddOrUpdateAsync(UserVisitSummaryIndex index)
    {
        if (index == null)
        {
            return;
        }
        await _userVisitSummaryRepository.AddOrUpdateAsync(index);
    }
}