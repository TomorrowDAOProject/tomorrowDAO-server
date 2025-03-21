using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.TonGift.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.TonGift.Provider;

public interface ITonGiftTaskProvider
{
    Task BulkAddOrUpdateAsync(List<TonGiftTaskIndex> list);
    Task<List<TonGiftTaskIndex>> GetByIdList(List<string> idList);
    Task<List<TonGiftTaskIndex>> GetCompletedByTaskIdAndIdentifierListAsync(string taskId, List<string> identifierList);
    Task<List<TonGiftTaskIndex>> GetNeedUpdateListAsync(string taskId, int skipCount);
}

public class TonGiftTaskProvider : ITonGiftTaskProvider, ISingletonDependency
{
    private readonly INESTRepository<TonGiftTaskIndex, string> _tonGiftTaskRepository;

    public TonGiftTaskProvider(INESTRepository<TonGiftTaskIndex, string> tonGiftTaskRepository)
    {
        _tonGiftTaskRepository = tonGiftTaskRepository;
    }

    public async Task BulkAddOrUpdateAsync(List<TonGiftTaskIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }

        await _tonGiftTaskRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<TonGiftTaskIndex>> GetByIdList(List<string> idList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TonGiftTaskIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.Identifier).Terms(idList)),
        };
        QueryContainer Filter(QueryContainerDescriptor<TonGiftTaskIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _tonGiftTaskRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<List<TonGiftTaskIndex>> GetCompletedByTaskIdAndIdentifierListAsync(string taskId, List<string> identifierList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TonGiftTaskIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.TaskId).Value(taskId)),
            q => q.Terms(i => i.Field(f => f.Identifier).Terms(identifierList)),
            q => q.Term(i => i.Field(f => f.UpdateTaskStatus).Value(UpdateTaskStatus.Completed))
        };
        QueryContainer Filter(QueryContainerDescriptor<TonGiftTaskIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _tonGiftTaskRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<List<TonGiftTaskIndex>> GetNeedUpdateListAsync(string taskId, int skipCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TonGiftTaskIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.TaskId).Value(taskId)),
            q => !q.Term(i => i.Field(f => f.UpdateTaskStatus).Value(UpdateTaskStatus.Completed))
        };
        QueryContainer Filter(QueryContainerDescriptor<TonGiftTaskIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _tonGiftTaskRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending,
            sortExp: o => o.Identifier)).Item2;
    }
}