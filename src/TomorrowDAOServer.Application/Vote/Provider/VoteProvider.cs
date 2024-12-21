using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Vote.Dto;
using TomorrowDAOServer.Vote.Index;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Vote.Provider;

public interface IVoteProvider
{
    Task<Dictionary<string, IndexerVote>> GetVoteItemsAsync(string chainId, List<string> votingItemIds);

    Task<List<WithdrawnDto>> GetVoteWithdrawnAsync(string chainId, string daoId, string voter);

    Task<List<IndexerVoteRecord>> GetLimitVoteRecordAsync(GetLimitVoteRecordInput input);
    Task<List<IndexerVoteRecord>> GetAllVoteRecordAsync(GetAllVoteRecordInput input);
    Task<List<IndexerVoteSchemeInfo>> GetVoteSchemeAsync(GetVoteSchemeInput input);

    Task<Dictionary<string, IndexerVoteSchemeInfo>> GetVoteSchemeDicAsync(GetVoteSchemeInput input);
    Task<List<IndexerVoteRecord>> GetSyncVoteRecordListAsync(GetChainBlockHeightInput input);
    Task<List<WithdrawnDto>> GetSyncVoteWithdrawListAsync(GetChainBlockHeightInput input);
    Task<List<VoteRecordIndex>> GetByVotingItemIdsAsync(string chainId, List<string> votingItemIds);

    Task<List<VoteRecordIndex>> GetByVoterAndVotingItemIdsAsync(string chainId, string voter,
        List<string> votingItemIds);

    Task<List<VoteRecordIndex>> GetByVotersAndVotingItemIdAsync(string chainId, List<string> voters,
        string votingItemId);

    Task<List<VoteRecordIndex>> GetNonWithdrawVoteRecordAsync(string chainId, string daoId, string voter);
    Task<Tuple<long, List<VoteRecordIndex>>> GetPageVoteRecordAsync(GetPageVoteRecordInput input);
    Task<IndexerDAOVoterRecord> GetDaoVoterRecordAsync(string chainId, string daoId, string voter);
    Task<long> GetVotePoints(string chainId, string daoId, string voter);
    Task<List<VoteRecordIndex>> GetNeedMoveVoteRecordListAsync();
    Task<List<string>> GetDistinctVotersAsync(string proposalId);
    Task<List<VoteRecordIndex>> GetByProposalIdAndHeightAsync(string proposalId, long blockHeight, int skipCount, int maxResultCount);
    Task<long> CountByVoterAndVotingItemIdAsync(string voter, string votingItemId);
    Task<long> CountByVoterAndTimeAsync(string voter, long time);
    Task<VoteRecordIndex> GetLatestByVoterAndVotingItemIdAsync(string voter, string votingItemId);
}

public class VoteProvider : IVoteProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ILogger<VoteProvider> _logger;
    private readonly INESTRepository<VoteRecordIndex, string> _voteRecordIndexRepository;

    public VoteProvider(IGraphQlHelper graphQlHelper, ILogger<VoteProvider> logger,
        INESTRepository<VoteRecordIndex, string> voteRecordIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
        _voteRecordIndexRepository = voteRecordIndexRepository;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleGetVoteItemsAsync))]
    public virtual async Task<Dictionary<string, IndexerVote>> GetVoteItemsAsync(string chainId, List<string> votingItemIds)
    {
        Stopwatch sw = Stopwatch.StartNew();
        if (votingItemIds.IsNullOrEmpty())
        {
            return new Dictionary<string, IndexerVote>();
        }

        var result = await _graphQlHelper.QueryAsync<IndexerVotes>(new GraphQLRequest
        {
            Query = @"
			    query($chainId: String,$votingItemIds: [String]!) {
                    data:getVoteItems(input:{chainId:$chainId,votingItemIds:$votingItemIds}) {
                        votingItemId,
                        voteSchemeId,
                        dAOId,
                        acceptedCurrency,
                        approvedCount,
                        rejectionCount,
                        abstentionCount,
                        votesAmount,
                        voterCount,
                        executer
                    }
                  }",
            Variables = new
            {
                chainId = chainId,
                votingItemIds = votingItemIds
            }
        });
        var voteItems = result.Data ?? new List<IndexerVote>();

        sw.Stop();
        Log.Information("ProposalListDuration: GetVoteItemsAsync {0}", sw.ElapsedMilliseconds);

        return voteItems.ToDictionary(vote => vote.VotingItemId, vote => vote);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetVoteWithdrawnAsync error",
        ReturnDefault = ReturnDefault.New, LogTargets = new []{"chainId", "daoId", "voter"})]
    public virtual async Task<List<WithdrawnDto>> GetVoteWithdrawnAsync(string chainId, string daoId, string voter)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerVoteWithdrawn>(new GraphQLRequest
        {
            Query = @"
			    query($chainId: String!,$daoId: String!,$voter: String!) {
                    dataList:getVoteWithdrawn(input:{chainId:$chainId,daoId:$daoId,voter:$voter}) {
                        votingItemIdList,
                        voter
                    }
                  }",
            Variables = new
            {
                chainId,
                daoId,
                voter
            }
        });
        return result?.DataList ?? new List<WithdrawnDto>();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetLimitVoteRecordInput error",
        ReturnDefault = ReturnDefault.New, LogTargets = new []{"input"})]
    public virtual async Task<List<IndexerVoteRecord>> GetLimitVoteRecordAsync(GetLimitVoteRecordInput input)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerVoteRecords>(new GraphQLRequest
        {
            Query = @"
			    query($chainId: String!,$votingItemId: String!,$voter: String,$sorting: String, $limit: Int!) {
                    dataList:getLimitVoteRecord(input:{chainId:$chainId,votingItemId:$votingItemId,voter:$voter,sorting:$sorting,limit:$limit}) {
                        voter,
                        amount,
                        option,
                        voteTime,
                        startTime,
                        endTime,
                        transactionId,
                        votingItemId,
                        voteMechanism
                    }
                  }",
            Variables = new
            {
                input.ChainId,
                input.VotingItemId,
                input.Voter,
                input.Sorting,
                input.Limit
            }
        });
        return result?.DataList ?? new List<IndexerVoteRecord>();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetAllVoteRecordAsync error",
        ReturnDefault = ReturnDefault.New, LogTargets = new []{"input"})]
    public virtual async Task<List<IndexerVoteRecord>> GetAllVoteRecordAsync(GetAllVoteRecordInput input)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerVoteRecords>(new GraphQLRequest
        {
            Query = @"
			    query($chainId: String!,$voter: String!,$dAOId: String!) {
                    dataList:getAllVoteRecord(input:{chainId:$chainId,voter:$voter,dAOId:$dAOId}) {
                        voter,
                        amount,
                        option,
                        voteTime,
                        startTime,
                        endTime,
                        transactionId,
                        votingItemId,
                        voteMechanism
                    }
                  }",
            Variables = new
            {
                chainId = input.ChainId,
                voter = input.Voter,
                dAOId = input.DAOId
            }
        });
        return result?.DataList ?? new List<IndexerVoteRecord>();
    }

    public async Task<List<IndexerVoteSchemeInfo>> GetVoteSchemeAsync(GetVoteSchemeInput input)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerVoteSchemeResult>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String){
            data:getVoteSchemes(input: {chainId:$chainId})
            {
                id,chainId,voteSchemeId,voteMechanism,voteStrategy,withoutLockToken,voteCount
            }}",
            Variables = new
            {
                chainId = input.ChainId,
            }
        });
        return graphQlResponse?.Data ?? new List<IndexerVoteSchemeInfo>();
    }

    public async Task<Dictionary<string, IndexerVoteSchemeInfo>> GetVoteSchemeDicAsync(GetVoteSchemeInput input)
    {
        var sw = Stopwatch.StartNew();
        var voteSchemeInfos = await GetVoteSchemeAsync(input);

        sw.Stop();
        Log.Information("ProposalListDuration: GetVoteSchemeDicAsync {0}", sw.ElapsedMilliseconds);

        return voteSchemeInfos.ToDictionary(x => x.VoteSchemeId, x => x);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetSyncVoteRecordListAsync error",
        ReturnDefault = ReturnDefault.New, LogTargets = new []{"input"})]
    public virtual async Task<List<IndexerVoteRecord>> GetSyncVoteRecordListAsync(GetChainBlockHeightInput input)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerVoteRecords>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    dataList:getVoteRecordList(input:{chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}) {
                        id,
                        blockHeight,   
                        chainId,                     
                        voter,
                        transactionId,
                        dAOId,
                        voteMechanism,
                        amount,
                        votingItemId,
                        option,
                        voteTime,
                        startTime,
                        endTime,
                        memo
                    }
                  }",
            Variables = new
            {
                chainId = input.ChainId, skipCount = input.SkipCount, maxResultCount = input.MaxResultCount,
                startBlockHeight = input.StartBlockHeight, endBlockHeight = input.EndBlockHeight
            }
        });
        return result?.DataList ?? new List<IndexerVoteRecord>();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetSyncVoteWithdrawListAsync error",
        ReturnDefault = ReturnDefault.New, LogTargets = new []{"input"})]
    public virtual async Task<List<WithdrawnDto>> GetSyncVoteWithdrawListAsync(GetChainBlockHeightInput input)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerVoteWithdrawn>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    dataList:getVoteWithdrawnList(input:{chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}) {
                        votingItemIdList,
                        voter,
                        blockHeight
                    }
                  }",
            Variables = new
            {
                chainId = input.ChainId, skipCount = input.SkipCount, maxResultCount = input.MaxResultCount,
                startBlockHeight = input.StartBlockHeight, endBlockHeight = input.EndBlockHeight
            }
        });
        return result?.DataList ?? new List<WithdrawnDto>();
    }

    public async Task<List<VoteRecordIndex>> GetByVotingItemIdsAsync(string chainId, List<string> votingItemIds)
    {
        if (votingItemIds.IsNullOrEmpty())
        {
            return new List<VoteRecordIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Terms(i =>
                i.Field(f => f.VotingItemId).Terms(votingItemIds))
        };

        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _voteRecordIndexRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<List<VoteRecordIndex>> GetByVotersAndVotingItemIdAsync(string chainId, List<string> voters,
        string votingItemId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(f => f.VotingItemId).Value(votingItemId)),
            q => q.Terms(i => i.Field(f => f.Voter).Terms(voters)),
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _voteRecordIndexRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<List<VoteRecordIndex>> GetNonWithdrawVoteRecordAsync(string chainId, string daoId, string voter)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(f => f.DAOId).Value(daoId)),
            q => q.Term(i => i.Field(f => f.Voter).Value(voter)),
            q => q.Term(i => i.Field(f => f.IsWithdraw).Value(false)),
            q => q.Term(i => i.Field(f => f.VoteMechanism).Value(VoteMechanism.TOKEN_BALLOT))
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _voteRecordIndexRepository);
    }

    public async Task<Tuple<long, List<VoteRecordIndex>>> GetPageVoteRecordAsync(GetPageVoteRecordInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId))
        };
        if (VoteHistorySource.Telegram.ToString() == input.Source)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ValidRankingVote).Value(true)));
        }

        if (!string.IsNullOrEmpty(input.VotingItemId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.VotingItemId).Value(input.VotingItemId)));
        }

        if (!string.IsNullOrEmpty(input.DaoId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.DAOId).Value(input.DaoId)));
        }

        if (!string.IsNullOrEmpty(input.Voter))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Voter).Value(input.Voter)));
        }

        if (!string.IsNullOrEmpty(input.VoteOption) && !Enum.TryParse<VoteOption>(input.VoteOption, out var option))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Option).Value(option)));
        }

        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _voteRecordIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: _ => new SortDescriptor<VoteRecordIndex>().Descending(index => index.BlockHeight));
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetDaoVoterRecordAsync error")]
    public virtual async Task<IndexerDAOVoterRecord> GetDaoVoterRecordAsync(string chainId, string daoId, string voter)
    {
        //TODO Exception Test
        //throw new SystemException("This is message");
                
        var response = await _graphQlHelper.QueryAsync<IndexerCommonResult<List<IndexerDAOVoterRecord>>>(
            new GraphQLRequest
            {
                Query =
                    @"query($chainId:String!,$daoId:String!,$voterAddress:String!) {
                        data:getDAOVoterRecord(input: {chainId:$chainId,daoId:$daoId,voterAddress:$voterAddress})
                        {
                            id,
                            daoId,
                            voterAddress,
                            count,
                            amount,
                            chainId
                        }
                    }",
                Variables = new
                {
                    ChainId = chainId, DaoId = daoId, VoterAddress = voter
                }
            });
        return response.Data.IsNullOrEmpty() ? new IndexerDAOVoterRecord() : response.Data[0];
    }

    public async Task<long> GetVotePoints(string chainId, string daoId, string voter)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(f => f.Voter).Value(voter)),
            q => q.Term(i => i.Field(f => f.DAOId).Value(daoId)),
            q => q.Term(i => i.Field(f => f.ValidRankingVote).Value(true))
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _voteRecordIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<List<VoteRecordIndex>> GetNeedMoveVoteRecordListAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ValidRankingVote).Value(true)),
            // q => q.Term(i => i.Field(f => f.TotalRecorded).Value(false))
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _voteRecordIndexRepository);
    }

    public async Task<List<string>> GetDistinctVotersAsync(string proposalId)
    {
        var query = new SearchDescriptor<VoteRecordIndex>()
            .Query(q => q.Bool(b => b
                .Must(m =>
                    m.Term(t => t.Field(f => f.VotingItemId).Value(proposalId)))))
            .Size(0)
            .Aggregations(a => a
                .Terms("distinct_voters", t => t.Field(f => f.Voter).Size(int.MaxValue)));
        var response = await _voteRecordIndexRepository.SearchAsync(query, 0, int.MaxValue);
        var distinctVoters = response.Aggregations.Terms("distinct_voters").Buckets
            .Select(b => b.Key as string).ToList();
        return distinctVoters;
    }

    public async Task<List<VoteRecordIndex>> GetByProposalIdAndHeightAsync(string proposalId, long blockHeight, int skipCount, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.VotingItemId).Value(proposalId)),
            q => q.LongRange(i => i.Field(f => f.BlockHeight).GreaterThanOrEquals(blockHeight))
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _voteRecordIndexRepository.GetSortListAsync(Filter, sortFunc: _ => new SortDescriptor<VoteRecordIndex>().Ascending(index => index.BlockHeight),
            skip: skipCount, limit: maxResultCount)).Item2;
    }

    public async Task<long> CountByVoterAndVotingItemIdAsync(string voter, string votingItemId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Voter).Value(voter)),
            q => q.Term(i => i.Field(f => f.VotingItemId).Value(votingItemId))
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _voteRecordIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<long> CountByVoterAndTimeAsync(string voter, long time)
    {
        if (string.IsNullOrEmpty(voter))
        {
            return 0;
        }
        var starTimeDate = DateTimeOffset.FromUnixTimeMilliseconds(time).DateTime;
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Voter).Value(voter)),
            q => q.DateRange(r => r.Field(f => f.VoteTime).GreaterThanOrEquals(starTimeDate))
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _voteRecordIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<VoteRecordIndex> GetLatestByVoterAndVotingItemIdAsync(string voter, string votingItemId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Voter).Value(voter)),
            q => q.Term(i => i.Field(f => f.VotingItemId).Value(votingItemId))
        };
        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _voteRecordIndexRepository.GetAsync(Filter, sortType: SortOrder.Descending,
            sortExp: o => o.VoteTime);
    }

    public async Task<List<VoteRecordIndex>> GetByVoterAndVotingItemIdsAsync(string chainId, string voter, List<string> votingItemIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(f => f.Voter).Value(voter)),
            q => q.Terms(i => i.Field(f => f.VotingItemId).Terms(votingItemIds))
        };
        if (votingItemIds != null && !votingItemIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.VotingItemId).Terms(votingItemIds)));
        }

        QueryContainer Filter(QueryContainerDescriptor<VoteRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _voteRecordIndexRepository.GetListAsync(Filter)).Item2;
    }
}