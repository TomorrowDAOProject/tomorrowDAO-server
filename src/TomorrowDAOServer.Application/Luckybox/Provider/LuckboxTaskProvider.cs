using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.LuckyBox.Provider;

public interface ILuckboxTaskProvider
{
    Task AddOrUpdateAsync(LuckyBoxTaskIndex index);
    Task BulkAddOrUpdateAsync(List<LuckyBoxTaskIndex> list);
    Task<LuckyBoxTaskIndex> GetByAddressAndProposalIdAsync(string address, string proposalId);
    Task GenerateTaskAsync(string address, string proposalId, string trackId, DateTime voteTime);
    Task<List<LuckyBoxTaskIndex>> GetNeedReportAsync(int skipCount);
}

public class LuckboxTaskProvider : ILuckboxTaskProvider, ISingletonDependency
{
    private readonly INESTRepository<LuckyBoxTaskIndex, string> _luckyboxTaskRepository;

    public LuckboxTaskProvider(INESTRepository<LuckyBoxTaskIndex, string> luckyboxTaskRepository)
    {
        _luckyboxTaskRepository = luckyboxTaskRepository;
    }

    public async Task AddOrUpdateAsync(LuckyBoxTaskIndex index)
    {
        await _luckyboxTaskRepository.AddOrUpdateAsync(index);
    }

    public async Task BulkAddOrUpdateAsync(List<LuckyBoxTaskIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        await _luckyboxTaskRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<LuckyBoxTaskIndex> GetByAddressAndProposalIdAsync(string address, string proposalId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<LuckyBoxTaskIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Address).Value(address)),
            q => q.Term(i => i.Field(f => f.ProposalId).Value(proposalId))
        };
        QueryContainer Filter(QueryContainerDescriptor<LuckyBoxTaskIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _luckyboxTaskRepository.GetAsync(Filter);
    }

    public async Task GenerateTaskAsync(string address, string proposalId, string trackId, DateTime voteTime)
    {
        var task = await GetByAddressAndProposalIdAsync(address, proposalId);
        if (task == null)
        {
            await AddOrUpdateAsync(new LuckyBoxTaskIndex
            {
                Id = GuidHelper.GenerateGrainId(proposalId, address), ProposalId = proposalId, Address = address,
                TrackId = trackId, UpdateTaskStatus = UpdateTaskStatus.Pending, CompleteTime = voteTime
            });
        }
    }

    public async Task<List<LuckyBoxTaskIndex>> GetNeedReportAsync(int skipCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<LuckyBoxTaskIndex>, QueryContainer>>
        {
            q => !q.Term(i => i.Field(f => f.UpdateTaskStatus).Value(UpdateTaskStatus.Completed)),
        };
        QueryContainer Filter(QueryContainerDescriptor<LuckyBoxTaskIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (_, list) = await _luckyboxTaskRepository.GetListAsync(Filter, sortType: SortOrder.Ascending,
            sortExp: o => o.CompleteTime, skip: skipCount, limit: 50);
        return list;
    }
}