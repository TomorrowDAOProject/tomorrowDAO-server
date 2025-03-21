using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Proposal.Provider;

public interface IProposalProvider
{
    Task<List<IndexerProposal>> GetSyncProposalDataAsync(int skipCount, string chainId, long startBlockHeight,
        long endBlockHeight, int maxResultCount);

    public Task<Tuple<long, List<ProposalIndex>>> GetProposalListAsync(QueryProposalListInput input, List<string> excludeIds = null);

    public Task<ProposalIndex> GetProposalByIdAsync(string chainId, string proposalId);

    public Task<List<ProposalIndex>> GetProposalByIdsAsync(string chainId, List<string> proposalIds);

    public Task<long> GetProposalCountByDAOId(string chainId, string DAOId);

    public Task<IDictionary<string, long>> GetProposalCountByDaoIds(string chainId, ISet<string> daoIds);

    public Task BulkAddOrUpdateAsync(List<ProposalIndex> list);

    public Task<List<ProposalIndex>> GetNonFinishedProposalListAsync(int skipCount, List<ProposalStage> stageList);

    public Task<List<ProposalIndex>> GetNeedChangeProposalListAsync(int skipCount);

    public Task<Tuple<long, List<ProposalIndex>>> QueryProposalsByProposerAsync(QueryProposalByProposerRequest request);
    public Task<ProposalIndex> GetDefaultProposalAsync(string chainId);
    public Task<Tuple<long, List<ProposalIndex>>> GetRankingProposalListAsync(string chainId, int skipCount, int maxResultCount, 
        RankingType rankingType, string excludeAddress, bool needActive, List<string> excludeProposalIds = null);
    public Task<ProposalIndex> GetTopProposalAsync(string proposer, bool isActive);
    Task<List<ProposalIndex>> GetActiveRankingProposalListAsync(List<string> rankingDaoIds);
    public Task<Tuple<long, List<ProposalIndex>>> GetPollListAsync(string chainId, int skipCount, int maxResultCount, 
        bool active, List<string> excludeProposalIds = null);
}

public class ProposalProvider : IProposalProvider, ISingletonDependency
{
    private readonly ILogger<ProposalProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<ProposalIndex, string> _proposalIndexRepository;

    public ProposalProvider(IGraphQlHelper graphQlHelper,
        INESTRepository<ProposalIndex, string> proposalIndexRepository, ILogger<ProposalProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _proposalIndexRepository = proposalIndexRepository;
        _logger = logger;
    }

    public async Task<List<IndexerProposal>> GetSyncProposalDataAsync(int skipCount, string chainId,
        long startBlockHeight, long endBlockHeight, int maxResultCount)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerProposalSync>(new GraphQLRequest
        {
            Query =
                @"query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!){
            dataList:getSyncProposalInfos(input: {skipCount:$skipCount,chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount})
            {
                id,chainId,blockHeight,
                dAOId,proposalId,proposalTitle,proposalDescription,forumUrl,proposalType,
                activeStartTime,activeEndTime,executeStartTime,executeEndTime,
                proposalStatus,proposalStage,proposer,schemeAddress,
                transaction {
                    toAddress,contractMethodName,params
                },            
                voteSchemeId,vetoProposalId,beVetoProposalId,deployTime,executeTime,
                governanceMechanism,
                minimalRequiredThreshold,minimalVoteThreshold,minimalApproveThreshold,
                maximalRejectionThreshold,maximalAbstentionThreshold,proposalThreshold,
                activeTimePeriod,vetoActiveTimePeriod,pendingTimePeriod,executeTimePeriod,vetoExecuteTimePeriod,isNetworkDAO
            }}",
            Variables = new
            {
                skipCount,
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount
            }
        });
        return graphQlResponse?.DataList ?? new List<IndexerProposal>();
    }

    public async Task<Tuple<long, List<ProposalIndex>>> GetProposalListAsync(QueryProposalListInput input, List<string> excludeIds = null)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        var contentShouldQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        var proposalShouldQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();
        AssemblyBaseQuery(input, mustQuery, excludeIds);
        AssemblyContentQuery(input.Content, contentShouldQuery);
        AssemblyProposalStatusQuery(input.ProposalStatus, mustQuery, proposalShouldQuery);

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => proposalShouldQuery.Any() || contentShouldQuery.Any()
                ? b.Must(mustQuery)
                    .Should(s => contentShouldQuery.Any() && proposalShouldQuery.Any()
                        ? s.Bool(sb => sb.Should(contentShouldQuery).MinimumShouldMatch(1))
                          && s.Bool(sb => sb.Should(proposalShouldQuery).MinimumShouldMatch(1))
                        : contentShouldQuery.Any()
                            ? s.Bool(sb => sb.Should(contentShouldQuery).MinimumShouldMatch(1))
                            : s.Bool(sb => sb.Should(proposalShouldQuery).MinimumShouldMatch(1)))
                    .MinimumShouldMatch(1)
                : b.Must(mustQuery)
            );

        var now = DateTime.UtcNow;
        return await _proposalIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount,
            sortFunc: _ => new SortDescriptor<ProposalIndex>().Descending(a => a.DeployTime));
    }

    public async Task<ProposalIndex> GetProposalByIdAsync(string chainId, string proposalId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(f => f.ProposalId).Value(proposalId))
        };

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _proposalIndexRepository.GetAsync(Filter);
    }

    public async Task<List<ProposalIndex>> GetProposalByIdsAsync(string chainId, List<string> proposalIds)
    {
        if (proposalIds.IsNullOrEmpty())
        {
            return new List<ProposalIndex>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)),
            q => q.Terms(i => i.Field(f => f.ProposalId).Terms(proposalIds))
        };

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));

        return (await _proposalIndexRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<Tuple<long, List<ProposalIndex>>> QueryProposalsByProposerAsync(
        QueryProposalByProposerRequest request)
    {
        if (request == null || request.Proposer.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Proposer).Value(request.Proposer))
        };

        if (!request.ChainId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(request.ChainId)));
        }

        if (!request.DaoId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.DAOId).Value(request.DaoId)));
        }

        if (request.ProposalStage != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ProposalStage).Value(request.ProposalStage)));
        }

        // if (request.ProposalStatus != null)
        // {
        //     mustQuery.Add(q => q.Term(i =>
        //         i.Field(f => f.ProposalStatus).Value(request.ProposalStatus)));
        // }

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _proposalIndexRepository.GetSortListAsync(Filter, sortFunc: _ => new SortDescriptor<ProposalIndex>().Ascending(index => index.DeployTime),
            skip: request.SkipCount,
            limit: request.MaxResultCount);
    }

    public async Task<ProposalIndex> GetDefaultProposalAsync(string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Terms(i =>
                i.Field(f => f.ChainId).Terms(chainId)), 
            q => q.Terms(i =>
                i.Field(f => f.ProposalCategory).Terms(ProposalCategory.Ranking)),
            q => q.DateRange(r => r
                .Field(f => f.ActiveStartTime).LessThan(DateTime.UtcNow))
        };
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _proposalIndexRepository.GetAsync(Filter, sortType: SortOrder.Descending,
            sortExp: o => o.DeployTime);
    }

    public async Task<Tuple<long, List<ProposalIndex>>> GetRankingProposalListAsync(string chainId, int skipCount, 
        int maxResultCount, RankingType rankingType, string excludeAddress, bool needActive, List<string> excludeProposalIds = null)
    {
        var now = DateTime.UtcNow;
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)), 
            q => q.Term(i => i.Field(f => f.ProposalCategory).Value(ProposalCategory.Ranking)),
            q => q.DateRange(i => i.Field(f => f.ActiveStartTime).LessThanOrEquals(now))
        };

        if (excludeProposalIds != null && !excludeProposalIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => !q.Terms(i => i.Field(f => f.ProposalId).Terms(excludeProposalIds)));
        }
        
        if (!string.IsNullOrWhiteSpace(excludeAddress))
        {
            mustQuery.Add(q => !q.Term(i => i.Field(f => f.Proposer).Value(excludeAddress)));
        }
        
        if (rankingType != RankingType.All)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.RankingType).Value(rankingType)));
        }

        if (needActive)
        {
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ActiveEndTime).GreaterThan(now)));
        }
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));

        var currentUtcTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"); 
        return await _proposalIndexRepository.GetSortListAsync(
            Filter,
            sortFunc: _ => new SortDescriptor<ProposalIndex>()
                .Script(script => script
                    .Type("number")
                    .Order(SortOrder.Descending)
                    .Script(s => s
                        .Source("doc['activeEndTime'].value.toInstant().toEpochMilli() > ZonedDateTime.parse(params.currentUtcTime).toInstant().toEpochMilli() " +
                                "&& doc['activeStartTime'].value.toInstant().toEpochMilli() < ZonedDateTime.parse(params.currentUtcTime).toInstant().toEpochMilli() ? 1 : 0")
                        .Params(p => p.Add("currentUtcTime", currentUtcTime))
                    )
                )
                .Descending(a => a.DeployTime),
            skip: skipCount,
            limit: maxResultCount
        );
    }

    public async Task<ProposalIndex> GetTopProposalAsync(string proposer, bool isActive)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Proposer).Value(proposer)),
            q => q.Term(i => i.Field(f => f.ProposalCategory).Value(ProposalCategory.Ranking)),
            q => q.Term(i => i.Field(f => f.RankingType).Value(RankingType.Verified))
        };

        if (isActive)
        {
            mustQuery.Add(q => q.DateRange(r => r
                .Field(f => f.ActiveStartTime).LessThanOrEquals(DateTime.UtcNow)));
            mustQuery.Add(q => q.DateRange(r => r
                .Field(f => f.ActiveEndTime).GreaterThan(DateTime.UtcNow)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _proposalIndexRepository.GetAsync(Filter, sortType: SortOrder.Descending,
            sortExp: o => o.DeployTime);
    }

    public async Task<List<ProposalIndex>> GetActiveRankingProposalListAsync(List<string> rankingDaoIds)
    {
        var currentDate = DateTime.UtcNow;
        var threeWeeksAgoDate = currentDate.AddDays(-21);
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.DateRange(r => r
                .Field(f => f.ActiveStartTime).GreaterThanOrEquals(threeWeeksAgoDate)),
            q => q.DateRange(r => r
                .Field(f => f.ActiveEndTime).LessThanOrEquals(currentDate)),
            q => q.Terms(r => r
                .Field(f => f.DAOId).Terms(rankingDaoIds)),
        };
        
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _proposalIndexRepository.GetListAsync(Filter)).Item2 ?? new List<ProposalIndex>();
    }

    public async Task<Tuple<long, List<ProposalIndex>>> GetPollListAsync(string chainId, int skipCount, int maxResultCount, bool active,
        List<string> excludeProposalIds = null)
    {
        var now = DateTime.UtcNow;
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(f => f.ProposalCategory).Value(ProposalCategory.Ranking))
        };
        if (excludeProposalIds != null && !excludeProposalIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => !q.Terms(i => i.Field(f => f.ProposalId).Terms(excludeProposalIds)));
        }

        if (active)
        {
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ActiveEndTime).GreaterThanOrEquals(now)));
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ActiveStartTime).LessThanOrEquals(now)));
        }
        else
        {
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ActiveEndTime).LessThanOrEquals(now)));
        }
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _proposalIndexRepository.GetSortListAsync(Filter, skip: skipCount, limit: maxResultCount,
            sortFunc: _ => new SortDescriptor<ProposalIndex>()
                .Script(s => s.Type("number").Order(SortOrder.Descending)
                    .Script(ss => ss.Source("doc['rankingType'].value == 'Verified' ? 1 : 0")))
                .Descending(a => a.DeployTime));
    }

    public async Task<long> GetProposalCountByDAOId(string chainId, string DAOId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.ChainId).Terms(chainId)));
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.DAOId).Terms(DAOId)));

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return (await _proposalIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<IDictionary<string, long>> GetProposalCountByDaoIds(string chainId, ISet<string> daoIds)
    {
        var result = new Dictionary<string, long>();

        var query = new SearchDescriptor<ProposalIndex>().Size(0)
            .Query(q => q.Term(t => t.Field(f => f.ChainId).Value(chainId)))
            .Query(q => q.Terms(t => t.Field(f => f.DAOId).Terms(daoIds)))
            .Aggregations(a => a.Terms("dao_ids", t => t.Field(f => f.DAOId).Size(Int32.MaxValue)
                .Aggregations(aa => aa.ValueCount("proposal_count", vc => vc
                    .Field(f => f.Id)))));

        var response = await _proposalIndexRepository.SearchAsync(query, 0, Int32.MaxValue);
        var daoIdsAgg = response.Aggregations.Terms("dao_ids");
        foreach (var bucket in daoIdsAgg.Buckets)
        {
            var daoId = bucket.Key;
            var count = bucket.ValueCount("proposal_count").Value;
            if (count.HasValue && (count.Value >= long.MinValue && count.Value <= long.MaxValue))  
            {  
                var safeLong = (long)count.Value;  
                result.Add(daoId, safeLong);  
            }  
            else  
            {  
                Log.Error("The number is too large or too small for a long.");  
                result.Add(daoId, 0);  
            }
        }

        return result;
    }

    public async Task BulkAddOrUpdateAsync(List<ProposalIndex> list)
    {
        await _proposalIndexRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<ProposalIndex>> GetNonFinishedProposalListAsync(int skipCount, List<ProposalStage> stageList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => !q.Terms(i =>
                i.Field(f => f.ProposalStage).Terms(stageList))
        };

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var tuple = await _proposalIndexRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending,
            sortExp: o => o.BlockHeight);
        return tuple.Item2;
    }

    public async Task<List<ProposalIndex>> GetNeedChangeProposalListAsync(int skipCount)
    {
        var currentStr = DateTime.UtcNow.ToString("O");
        var activeMustQuery = GetNeedChangeMustQuery(ProposalStage.Active,
            tr => tr.Field(f => f.ActiveEndTime.ToUtcMilliSeconds()).LessThan(currentStr));
        var pendingMustQuery = GetNeedChangeMustQuery(ProposalStage.Pending,
            tr => tr.Field(f => f.ExecuteStartTime.ToUtcMilliSeconds()).LessThan(currentStr));
        ;
        var executeMustQuery = GetNeedChangeMustQuery(ProposalStage.Execute,
            tr => tr.Field(f => f.ExecuteEndTime.ToUtcMilliSeconds()).LessThan(currentStr));
        ;
        var shouldQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Bool(b => b.Must(activeMustQuery)),
            q => q.Bool(b => b.Must(pendingMustQuery)),
            q => q.Bool(b => b.Must(executeMustQuery))
        };

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Should(shouldQuery).MinimumShouldMatch(1));

        var tuple = await _proposalIndexRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending,
            sortExp: o => o.BlockHeight);
        return tuple.Item2;
    }

    private List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> GetNeedChangeMustQuery(
        ProposalStage proposalStage, Func<TermRangeQueryDescriptor<ProposalIndex>, ITermRangeQuery> selector)
    {
        return new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(t => t.Field(f => f.ProposalStage).Value(proposalStage)),
            q => q.TermRange(selector)
        };
    }

    private static void AssemblyBaseQuery(QueryProposalListInput input,
        List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> mustQuery, List<string> excludeIds)
    {
        if (excludeIds != null && !excludeIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => !q.Terms(i => i.Field(f => f.ProposalId).Terms(excludeIds)));
        }
        if (!input.ChainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        if (!input.DaoId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.DAOId).Value(input.DaoId)));
        }

        if (input.GovernanceMechanism != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.GovernanceMechanism).Value(input.GovernanceMechanism)));
        }

        if (input.ProposalType != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalType).Value(input.ProposalType)));
        }
    }

    private static void AssemblyContentQuery(string content,
        List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> shouldQuery)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalId).Query(content)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalTitle).Query(content)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalDescription).Query(content)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.Proposer).Query(content)));
    }

    private static void AssemblyProposalStatusQuery(ProposalStatus? proposalStatus,
        ICollection<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> mustQuery,
        ICollection<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> shouldQuery)
    {
        switch (proposalStatus)
        {
            case null:
                return;
            case ProposalStatus.Defeated:
            {
                shouldQuery.Add(q => q.Bool(b => b.Must(ProposalStatusMustQuery(ProposalStatus.Rejected))));
                shouldQuery.Add(q => q.Bool(b => b.Must(ProposalStatusMustQuery(ProposalStatus.Abstained))));
                shouldQuery.Add(q => q.Bool(b => b.Must(ProposalStatusMustQuery(ProposalStatus.BelowThreshold))));
                break;
            }
            case ProposalStatus.PendingVote:
                mustQuery.Add(q => q.Term(i =>
                    i.Field(f => f.ProposalStatus).Value(proposalStatus)));
                mustQuery.Add(q => q.DateRange(d => d
                    .Field(f => f.ActiveStartTime).LessThanOrEquals(DateTime.UtcNow)));
                break;
            case ProposalStatus.Published:
                mustQuery.Add(q => q.Term(i =>
                    i.Field(f => f.ProposalStatus).Value(ProposalStatus.PendingVote)));
                mustQuery.Add(q => q.DateRange(d => d
                    .Field(f => f.ActiveStartTime).GreaterThanOrEquals(DateTime.UtcNow)));
                break;
            default:
                mustQuery.Add(q => q.Term(i =>
                    i.Field(f => f.ProposalStatus).Value(proposalStatus)));
                break;
        }
    }

    private static IEnumerable<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> ProposalStatusMustQuery(
        ProposalStatus proposalStatus)
    {
        return new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(i =>
                i.Field(f => f.ProposalStatus).Value(proposalStatus))
        };
    }
}