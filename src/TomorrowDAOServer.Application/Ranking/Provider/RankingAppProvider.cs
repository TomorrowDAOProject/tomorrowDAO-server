using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Ranking.Provider;

public interface IRankingAppProvider
{
    Task BulkAddOrUpdateAsync(List<RankingAppIndex> list);
    Task<List<RankingAppIndex>> GetByProposalIdAsync(string chainId, string proposalId);
    Task<RankingAppIndex> GetByProposalIdAndAliasAsync(string chainId, string proposalId, string alias);
    Task UpdateAppVoteAmountAsync(string chainId, string proposalId, string alias, long amount = 1);
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
}