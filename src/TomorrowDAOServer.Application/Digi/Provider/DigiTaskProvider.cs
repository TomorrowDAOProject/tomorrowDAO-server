using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Digi.Provider;

public interface IDigiTaskProvider
{
    Task AddOrUpdateAsync(DigiTaskIndex index);
    Task BulkAddOrUpdateAsync(List<DigiTaskIndex> list);
    Task<DigiTaskIndex> GetByAddressAndStartTimeAsync(string address, long startTime);
    Task GenerateTaskAsync(string address, long startTime, DateTime voteTime);
    Task<List<DigiTaskIndex>> GetNeedReportAsync(int skipCount, long startTime);
}

public class DigiTaskProvider: IDigiTaskProvider, ISingletonDependency
{
    private readonly INESTRepository<DigiTaskIndex, string> _digiTaskRepository;

    public DigiTaskProvider(INESTRepository<DigiTaskIndex, string> digiTaskRepository)
    {
        _digiTaskRepository = digiTaskRepository;
    }

    public async Task AddOrUpdateAsync(DigiTaskIndex index)
    {
        await _digiTaskRepository.AddOrUpdateAsync(index);
    }

    public async Task BulkAddOrUpdateAsync(List<DigiTaskIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        await _digiTaskRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<DigiTaskIndex> GetByAddressAndStartTimeAsync(string address, long startTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DigiTaskIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Address).Value(address)),
            q => q.Term(i => i.Field(f => f.StartTime).Value(startTime))
        };
        QueryContainer Filter(QueryContainerDescriptor<DigiTaskIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _digiTaskRepository.GetAsync(Filter);
    }

    public async Task GenerateTaskAsync(string address, long startTime, DateTime voteTime)
    {
        var task = await GetByAddressAndStartTimeAsync(address, startTime);
        if (task == null)
        {
            await AddOrUpdateAsync(new DigiTaskIndex
            {
                Id = GuidHelper.GenerateGrainId(address, startTime), TelegramId = string.Empty, Address = address,
                StartTime = startTime, UpdateTaskStatus = UpdateTaskStatus.Pending, CompleteTime = voteTime
            });
        }
    }

    public async Task<List<DigiTaskIndex>> GetNeedReportAsync(int skipCount, long startTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DigiTaskIndex>, QueryContainer>>
        {
            q => !q.Term(i => i.Field(f => f.UpdateTaskStatus).Value(UpdateTaskStatus.Completed)),
            q => q.Term(i => i.Field(f => f.StartTime).Value(startTime))
        };
        QueryContainer Filter(QueryContainerDescriptor<DigiTaskIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (_, list) = await _digiTaskRepository.GetListAsync(Filter, sortType: SortOrder.Ascending,
            sortExp: o => o.CompleteTime, skip: skipCount, limit: 20);
        return list;
    }
}