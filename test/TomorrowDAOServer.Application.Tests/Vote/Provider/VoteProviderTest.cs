using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Vote.Dto;
using TomorrowDAOServer.Vote.Index;
using Xunit;

namespace TomorrowDAOServer.Vote.Provider;

public class VoteProviderTest
{ 
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ILogger<VoteProvider> _logger;
    private readonly INESTRepository<VoteRecordIndex, string> _voteRecordIndexRepository;
    private readonly IVoteProvider _provider;

    public VoteProviderTest()
    {
        _graphQlHelper = Substitute.For<IGraphQlHelper>();;
        _voteRecordIndexRepository = Substitute.For<INESTRepository<VoteRecordIndex, string>>();;
        _logger = Substitute.For<ILogger<VoteProvider>>();;
        _provider = new VoteProvider(_graphQlHelper, _logger, _voteRecordIndexRepository);
    }

    [Fact]
    public async void GetVoteInfosMemoryAsync_Test()
    {
        var result = await _provider.GetVoteItemsAsync("chainId", new List<string>());
        result.Count.ShouldBe(0);

        _graphQlHelper.QueryAsync<IndexerVotes>(Arg.Any<GraphQLRequest>())
            .Returns(new IndexerVotes { Data = new List<IndexerVote> { new() { VotingItemId = "votingItemId" } } });
        result = await _provider.GetVoteItemsAsync("chainId", new List<string>{"id"});
        result.Count.ShouldBe(1);
    }

    [Fact]
    public async void GetVoteWithdrawnAsync_Test()
    {
        var result = await _provider.GetVoteWithdrawnAsync("chainId", "daoId", "voter");
        _graphQlHelper.QueryAsync<IndexerVoteWithdrawn>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteWithdrawn());
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async void GetLimitVoteRecordAsync_Test()
    {
        var result = await _provider.GetLimitVoteRecordAsync(new GetLimitVoteRecordInput());
        _graphQlHelper.QueryAsync<IndexerVoteRecords>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteRecords());
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async void GetAllVoteRecordAsync_Test()
    {
        var result = await _provider.GetAllVoteRecordAsync(new GetAllVoteRecordInput());
        _graphQlHelper.QueryAsync<IndexerVoteRecords>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteRecords());
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetPageVoteRecordAsync_Test()
    {
        var result = await _provider.GetAllVoteRecordAsync(new GetAllVoteRecordInput());
        _graphQlHelper.QueryAsync<IndexerVoteRecords>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteRecords());
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetVoteSchemeAsync_Test()
    {
        var result = await _provider.GetVoteSchemeAsync(new GetVoteSchemeInput());
        _graphQlHelper.QueryAsync<IndexerVoteSchemeResult>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteSchemeResult());
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetVoteSchemeDicAsync_Test()
    {
        var result = await _provider.GetVoteSchemeDicAsync(new GetVoteSchemeInput());
        _graphQlHelper.QueryAsync<IndexerVoteSchemeResult>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteSchemeResult());
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetSyncVoteRecordListAsync_Test()
    {
        var result = await _provider.GetSyncVoteRecordListAsync(new GetChainBlockHeightInput());
        _graphQlHelper.QueryAsync<IndexerVoteRecords>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteRecords());
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetSyncVoteWithdrawListAsync_Test()
    {
        var result = await _provider.GetSyncVoteWithdrawListAsync(new GetChainBlockHeightInput());
        _graphQlHelper.QueryAsync<IndexerVoteWithdrawn>(Arg.Any<GraphQLRequest>()).Returns(new IndexerVoteWithdrawn());
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetByVotingItemIdsAsync_Test()
    {
        var result = await _provider.GetByVotingItemIdsAsync("chainId", new List<string>());
        result.Count.ShouldBe(0);
        
        _voteRecordIndexRepository.GetListAsync(
                Arg.Any<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>(),
                Arg.Any<Func<SourceFilterDescriptor<VoteRecordIndex>, ISourceFilter>>(),
                Arg.Any<Expression<Func<VoteRecordIndex, object>>>(), Arg.Any<SortOrder>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<VoteRecordIndex>>(0, new List<VoteRecordIndex>()));
        result = await _provider.GetByVotingItemIdsAsync("chainId", new List<string>{"id"});
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetNonWithdrawVoteRecordAsync_Test()
    {
        _voteRecordIndexRepository.GetListAsync(
                Arg.Any<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>(),
                Arg.Any<Func<SourceFilterDescriptor<VoteRecordIndex>, ISourceFilter>>(),
                Arg.Any<Expression<Func<VoteRecordIndex, object>>>(), Arg.Any<SortOrder>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<VoteRecordIndex>>(0, new List<VoteRecordIndex>()));
        var result = await _provider.GetNonWithdrawVoteRecordAsync("chainId", "daoId", "voter");
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public async void GetDaoVoterRecordAsync_Test()
    {
        var result = await _provider.GetDaoVoterRecordAsync("chainId", "daoId", "voter");
        _graphQlHelper.QueryAsync<IndexerCommonResult<List<IndexerDAOVoterRecord>>>(Arg.Any<GraphQLRequest>()).Returns(new IndexerCommonResult<List<IndexerDAOVoterRecord>>());
        result.Count.ShouldBe(0);
    }
}