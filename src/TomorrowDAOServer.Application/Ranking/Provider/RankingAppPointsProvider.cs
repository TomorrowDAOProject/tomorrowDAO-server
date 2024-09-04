using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Eto;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Ranking.Provider;

public interface IRankingAppPointsProvider
{
    Task AddOrUpdateAppPointsIndexAsync(VoteAndLikeMessageEto message);
    Task AddOrUpdateUserPointsIndexAsync(VoteAndLikeMessageEto message);
    Task AddOrUpdateAppPointsIndexAsync(RankingAppPointsIndex index);
    Task AddOrUpdateUserPointsIndexAsync(RankingAppUserPointsIndex index);

    Task<RankingAppPointsIndex> GetRankingAppPointsIndexByAliasAsync(string chainId, string proposalId,
        string alias = null, PointsType type = PointsType.All);

    Task<RankingAppUserPointsIndex> GetRankingUserPointsIndexByAliasAsync(string chainId, string proposalId,
        string address, string alias = null, PointsType type = PointsType.All);
}

public class RankingAppPointsProvider : IRankingAppPointsProvider, ISingletonDependency
{
    private ILogger<RankingAppPointsProvider> _logger;
    private readonly INESTRepository<RankingAppPointsIndex, Guid> _appPointsIndexRepository;
    private readonly INESTRepository<RankingAppUserPointsIndex, Guid> _userPointsIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;

    private readonly SemaphoreSlim _appPointsSemaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _userPointsSemaphore = new SemaphoreSlim(1, 1);

    public RankingAppPointsProvider(ILogger<RankingAppPointsProvider> logger,
        INESTRepository<RankingAppPointsIndex, Guid> appPointsIndexRepository,
        INESTRepository<RankingAppUserPointsIndex, Guid> userPointsIndexRepository, IObjectMapper objectMapper,
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider)
    {
        _logger = logger;
        _appPointsIndexRepository = appPointsIndexRepository;
        _userPointsIndexRepository = userPointsIndexRepository;
        _objectMapper = objectMapper;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
    }

    public async Task AddOrUpdateAppPointsIndexAsync(VoteAndLikeMessageEto message)
    {
        if (message.PointsType == PointsType.All)
        {
            return;
        }

        await _appPointsSemaphore.WaitAsync();
        try
        {
            var appPointsIndex = await GetRankingAppPointsIndexByAliasAsync(message.ChainId, message.ProposalId,
                message.Alias, message.PointsType);
            if (appPointsIndex == null || appPointsIndex.Id == Guid.Empty)
            {
                appPointsIndex = _objectMapper.Map<VoteAndLikeMessageEto, RankingAppPointsIndex>(message);
                appPointsIndex.Id = Guid.NewGuid();
            }
            else
            {
                appPointsIndex.Amount += message.Amount;
            }

            appPointsIndex.Points += message.PointsType == PointsType.Vote
                ? _rankingAppPointsCalcProvider.CalculatePointsFromVotes(message.Amount)
                : _rankingAppPointsCalcProvider.CalculatePointsFromLikes(message.Amount);
            appPointsIndex.UpdateTime = DateTime.Now;

            await AddOrUpdateAppPointsIndexAsync(appPointsIndex);
        }
        finally
        {
            _appPointsSemaphore.Release();
        }
    }

    public async Task AddOrUpdateUserPointsIndexAsync(VoteAndLikeMessageEto message)
    {
        if (message.PointsType == PointsType.All)
        {
            return;
        }

        await _userPointsSemaphore.WaitAsync();
        try
        {
            var userPointsIndex = await GetRankingUserPointsIndexByAliasAsync(message.ChainId, message.ProposalId,
                message.Address, message.Alias, message.PointsType);
            if (userPointsIndex == null || userPointsIndex.Id == Guid.Empty)
            {
                userPointsIndex = _objectMapper.Map<VoteAndLikeMessageEto, RankingAppUserPointsIndex>(message);
                userPointsIndex.Id = Guid.NewGuid();
            }
            else
            {
                userPointsIndex.Amount += message.Amount;
            }
            
            userPointsIndex.Points += message.PointsType == PointsType.Vote
                ? _rankingAppPointsCalcProvider.CalculatePointsFromVotes(message.Amount)
                : _rankingAppPointsCalcProvider.CalculatePointsFromLikes(message.Amount);
            userPointsIndex.UpdateTime = DateTime.Now;

            await AddOrUpdateUserPointsIndexAsync(userPointsIndex);
        }
        finally
        {
            _userPointsSemaphore.Release();
        }
    }

    public async Task AddOrUpdateAppPointsIndexAsync(RankingAppPointsIndex index)
    {
        await _appPointsIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateUserPointsIndexAsync(RankingAppUserPointsIndex index)
    {
        await _userPointsIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<RankingAppPointsIndex> GetRankingAppPointsIndexByAliasAsync(string chainId, string proposalId,
        string alias = null, PointsType type = PointsType.All)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppPointsIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ProposalId).Value(proposalId)));

        if (!alias.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Alias).Value(alias)));
        }

        if (type != PointsType.All)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PointsType).Value(type)));
        }

        QueryContainer Filter(QueryContainerDescriptor<RankingAppPointsIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _appPointsIndexRepository.GetAsync(Filter);
    }

    public async Task<RankingAppUserPointsIndex> GetRankingUserPointsIndexByAliasAsync(string chainId,
        string proposalId,
        string address, string alias = null, PointsType type = PointsType.All)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RankingAppUserPointsIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ProposalId).Value(proposalId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));

        if (!alias.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Alias).Value(alias)));
        }

        if (type != PointsType.All)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.PointsType).Value(type)));
        }

        QueryContainer Filter(QueryContainerDescriptor<RankingAppUserPointsIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _userPointsIndexRepository.GetAsync(Filter);
    }
}