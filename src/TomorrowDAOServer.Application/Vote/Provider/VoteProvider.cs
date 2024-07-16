using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
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
    
    Task<List<IndexerVoteRecord>> GetPageVoteRecordAsync(GetPageVoteRecordInput input);
    
    Task<List<IndexerVoteSchemeInfo>> GetVoteSchemeAsync(GetVoteSchemeInput input);

    Task<IDictionary<string, IndexerVoteSchemeInfo>> GetVoteSchemeDicAsync(GetVoteSchemeInput input);
    Task<List<IndexerVoteRecord>> GetSyncVoteRecordListAsync(GetChainBlockHeightInput input);
    Task<List<WithdrawnDto>> GetSyncVoteWithdrawListAsync(GetChainBlockHeightInput input);
    Task<List<VoteRecordIndex>> GetByVotingItemIdsAsync(string chainId, List<string> votingItemIds);
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

    public async Task<Dictionary<string, IndexerVote>> GetVoteItemsAsync(string chainId, List<string> votingItemIds)
    {
        if (votingItemIds.IsNullOrEmpty())
        {
            return new Dictionary<string, IndexerVote>();
        }

        try
        {
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
            var voteItems = result.Data?? new List<IndexerVote>();
            return voteItems.ToDictionary(vote => vote.VotingItemId, vote => vote);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetVoteItemsAsync Exception chainId {chainId}, votingItemIds {votingItemIds}", chainId, votingItemIds);
            return new Dictionary<string, IndexerVote>();
        }
    }
    
    public async Task<List<WithdrawnDto>> GetVoteWithdrawnAsync(string chainId, string daoId, string voter)
    {
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, "GetVoteWithdrawnAsync Exception chainId {chainId}, daoId {daoId}, voter {voter}", chainId, daoId, voter);
            return new List<WithdrawnDto>();
        }
    }

    public async Task<List<IndexerVoteRecord>> GetLimitVoteRecordAsync(GetLimitVoteRecordInput input)
    {
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, "GetLimitVoteRecordAsync Exception chainId {chainId}, votingItemId {votingItemId}, voter {voter}, sorting {sorting}", 
                input.ChainId, input.VotingItemId, input.Voter, input.Sorting);
            return new List<IndexerVoteRecord>();
        }
    }

    public async Task<List<IndexerVoteRecord>> GetAllVoteRecordAsync(GetAllVoteRecordInput input)
    {
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, "GetAllVoteRecordAsync Exception chainId {chainId}, daoId {daoId}, voter {voter}", input.ChainId, input.DAOId, input.Voter);
            return new List<IndexerVoteRecord>();
        }
    }

    public async Task<List<IndexerVoteRecord>> GetPageVoteRecordAsync(GetPageVoteRecordInput input)
    {
        try
        {
            var result = await _graphQlHelper.QueryAsync<IndexerVoteRecords>(new GraphQLRequest
            {
                Query = @"
                            query($chainId: String!, $daoId: String!, $voter: String!, $votingItemId: String, $skipCount: Int!, $maxResultCount: Int!, $voteOption: String) {
                                dataList: getPageVoteRecord(input: {
                                    chainId: $chainId,
                                    daoId: $daoId,
                                    voter: $voter,
                                    votingItemId: $votingItemId,
                                    skipCount: $skipCount,
                                    maxResultCount: $maxResultCount,
                                    voteOption: $voteOption
                                }) {
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
                    input.DaoId,
                    input.Voter,
                    input.VotingItemId,
                    input.SkipCount,
                    input.MaxResultCount,
                    input.VoteOption
                }
            });
            return result?.DataList ?? new List<IndexerVoteRecord>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetAddressVoteRecordAsync Exception chainId {chainId}, voter {voter}", input.ChainId, input.Voter);
            return new List<IndexerVoteRecord>();
        }
    }

    public async Task<List<IndexerVoteSchemeInfo>> GetVoteSchemeAsync(GetVoteSchemeInput input)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerVoteSchemeResult>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String){
            data:getVoteSchemes(input: {chainId:$chainId})
            {
                id,chainId,voteSchemeId,voteMechanism
            }}",
            Variables = new 
            {
                chainId = input.ChainId,
            }
        });
        return graphQlResponse?.Data ?? new List<IndexerVoteSchemeInfo>();
    }

    public async Task<IDictionary<string, IndexerVoteSchemeInfo>> GetVoteSchemeDicAsync(GetVoteSchemeInput input)
    {
        var voteSchemeInfos = await GetVoteSchemeAsync(input);
        return voteSchemeInfos.ToDictionary(x => x.VoteSchemeId, x => x);
    }

    public async Task<List<IndexerVoteRecord>> GetSyncVoteRecordListAsync(GetChainBlockHeightInput input)
    {
        try
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
                        endTime
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
        catch (Exception e)
        {
            _logger.LogError(e, "GetSyncVoteRecordListAsync Exception chainId {chainId}", input.ChainId);
            return new List<IndexerVoteRecord>();
        }
    }

    public async Task<List<WithdrawnDto>> GetSyncVoteWithdrawListAsync(GetChainBlockHeightInput input)
    {
        try
        {
            var result = await _graphQlHelper.QueryAsync<IndexerVoteWithdrawn>(new GraphQLRequest
            {
                Query = @"
			    query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    dataList:getVoteWithdrawnList(input:{chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}) {
                        votingItemIdList,
                        voter
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
        catch (Exception e)
        {
            _logger.LogError(e, "GetSyncVoteWithdrawListAsync Exception chainId {chainId}", input.ChainId);
            return new List<WithdrawnDto>();
        }
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
}