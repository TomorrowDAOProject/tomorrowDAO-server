using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider;

public interface IRankingAppProvider
{
    Task BulkAddOrUpdateAsync(List<RankingAppIndex> list);
    Task<Tuple<long, List<RankingAppIndex>>> GetRankingAppListAsync(GetRankingAppListInput input);
    Task<List<RankingAppIndex>> GetByProposalIdAsync(string chainId, string proposalId);
    Task<RankingAppIndex> GetByProposalIdAndAliasAsync(string chainId, string proposalId, string alias);
    Task UpdateAppVoteAmountAsync(string chainId, string proposalId, string alias, long amount = 1);
    Task<List<RankingAppIndex>> GetNeedMoveRankingAppListAsync();
    Task<List<RankingAppIndex>> GetByAliasAsync(string chainId, List<string> aliases);
    [Obsolete("Only use it once during data migration.")]
    Task<List<RankingAppIndex>> GetAllRankingAppAsync(string chainId);
}

public class RankingAppProvider : IRankingAppProvider, ISingletonDependency
{
    private readonly ILogger<RankingAppProvider> _logger;
    private readonly INESTRepository<RankingAppIndex, string> _rankingAppIndexRepository;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public RankingAppProvider(INESTRepository<RankingAppIndex, string> rankingAppIndexRepository,
        ILogger<RankingAppProvider> logger)
    {
        _rankingAppIndexRepository = rankingAppIndexRepository;
        _logger = logger;
    }

    public async Task BulkAddOrUpdateAsync(List<RankingAppIndex> list)
    {
        await _rankingAppIndexRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<Tuple<long, List<RankingAppIndex>>> GetRankingAppListAsync(GetRankingAppListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppIndex>, QueryContainer>>
        {
            q => q.Term(i =>
                i.Field(f => f.ChainId).Value(input.ChainId))
        };

        if (!input.Category.IsNullOrWhiteSpace() &&
            Enum.TryParse<TelegramAppCategory>(input.Category, true, out var categoryEnum) &&
            categoryEnum != TelegramAppCategory.All)
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.Categories).Terms(categoryEnum)));
        }

        if (!input.Search.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Title).Value(input.Search)));
        }
        
        if (!input.ProposalId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalId).Value(input.ProposalId)));
        }
        QueryContainer Filter(QueryContainerDescriptor<RankingAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        IPromise<IList<ISort>> SortDescriptor(SortDescriptor<RankingAppIndex> s) { return s.Descending(p => p.TotalPoints); }
        return await _rankingAppIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount, sortFunc: SortDescriptor);
    }

    public async Task<List<RankingAppIndex>> GetByProposalIdAsync(string chainId, string proposalId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppIndex>, QueryContainer>>
        {
            q => q.Terms(i =>
                i.Field(f => f.ChainId).Terms(chainId)),
            q => q.Terms(i =>
                i.Field(f => f.ProposalId).Terms(proposalId))
        };
        QueryContainer Filter(QueryContainerDescriptor<RankingAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return (await _rankingAppIndexRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<RankingAppIndex> GetByProposalIdAndAliasAsync(string chainId, string proposalId, string alias)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppIndex>, QueryContainer>>
        {
            q => q.Terms(i =>
                i.Field(f => f.ChainId).Terms(chainId)),
            q => q.Terms(i =>
                i.Field(f => f.ProposalId).Terms(proposalId)),
            q => q.Terms(i =>
                i.Field(f => f.Alias).Terms(alias))
        };
        QueryContainer Filter(QueryContainerDescriptor<RankingAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _rankingAppIndexRepository.GetAsync(Filter);
    }

    public async Task UpdateAppVoteAmountAsync(string chainId, string proposalId, string alias, long amount = 1)
    {
        await _semaphore.WaitAsync();
        try
        {
            var rankingAppIndex = await GetByProposalIdAndAliasAsync(chainId, proposalId, alias);
            if (rankingAppIndex != null && !rankingAppIndex.Id.IsNullOrWhiteSpace())
            {
                rankingAppIndex.VoteAmount += amount;
            }

            await BulkAddOrUpdateAsync(new List<RankingAppIndex>() { rankingAppIndex });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<RankingAppIndex>> GetNeedMoveRankingAppListAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppIndex>, QueryContainer>>
        {
            q => q.Range(r => r
                .Field(f => f.VoteAmount).GreaterThan(0))
        };
        QueryContainer Filter(QueryContainerDescriptor<RankingAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return (await _rankingAppIndexRepository.GetListAsync(Filter)).Item2;
    }
    
    public async Task<List<RankingAppIndex>> GetByAliasAsync(string chainId, List<string> aliases)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppIndex>, QueryContainer>>
        {
            q => q.Term(i =>
                i.Field(f => f.ChainId).Value(chainId)),
            q => q.Terms(i =>
                i.Field(f => f.Alias).Terms(aliases))
        };
        QueryContainer Filter(QueryContainerDescriptor<RankingAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await IndexHelper.GetAllIndex<RankingAppIndex>(Filter, _rankingAppIndexRepository);
    }

    [Obsolete("Only use it once during data migration.")]
    public async Task<List<RankingAppIndex>> GetAllRankingAppAsync(string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppIndex>, QueryContainer>>
        {
            q => q.Term(i =>
                i.Field(f => f.ChainId).Value(chainId)),
        };
        QueryContainer Filter(QueryContainerDescriptor<RankingAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await IndexHelper.GetAllIndex<RankingAppIndex>(Filter, _rankingAppIndexRepository);
    }
}