using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Nest;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using Xunit;

namespace TomorrowDAOServer.Proposal.Provider;

public sealed class ProposalProviderTest 
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<ProposalIndex, string> _proposalIndexRepository;
    private readonly IProposalProvider _provider;

    public ProposalProviderTest()
    {
        _graphQlHelper = Substitute.For<IGraphQlHelper>();;
        _proposalIndexRepository = Substitute.For<INESTRepository<ProposalIndex, string>>();
        _provider = new ProposalProvider(_graphQlHelper, _proposalIndexRepository);
    }

    [Fact]
    public async void GetSyncProposalDataAsync_Test()
    {
        _graphQlHelper.QueryAsync<IndexerProposalSync>(Arg.Any<GraphQLRequest>())
            .Returns(new IndexerProposalSync());
        var result = await _provider.GetSyncProposalDataAsync(0, "AELF", 0, 0, 100);
        result.ShouldNotBeNull();
        
        _graphQlHelper.QueryAsync<IndexerProposalSync>(Arg.Any<GraphQLRequest>())
            .Returns(new IndexerProposalSync{DataList = new List<IndexerProposal>()});
        result.ShouldNotBeNull();
    }

    [Fact]
    public async void GetProposalListAsync_Test()
    {
        _proposalIndexRepository.GetSortListAsync(
                Arg.Any<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
                Arg.Any<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
                Arg.Any<Func<SortDescriptor<ProposalIndex>, IPromise<IList<ISort>>>>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex> { new() }));
        var result = await _provider.GetProposalListAsync(new QueryProposalListInput
        {
            ChainId = "AELF", DaoId = "DaoId", GovernanceMechanism = GovernanceMechanism.Organization,
            ProposalType = ProposalType.Advisory, ProposalStatus = ProposalStatus.Abstained
        });
        result.ShouldNotBeNull();
    }

    [Fact]
    public async void GetProposalByIdAsync_Test()
    {
        var result = await _provider.GetProposalByIdAsync("chainId", "proposalId");
        result.ShouldBeNull();
    }

    [Fact]
    public async void GetProposalByIdsAsync_Test()
    {
        _proposalIndexRepository.GetListAsync(
                Arg.Any<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
                Arg.Any<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
                Arg.Any<Expression<Func<ProposalIndex, object>>>(), Arg.Any<SortOrder>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex> { new() }));
        var result = await _provider.GetProposalByIdsAsync("chainId", new List<string>{"proposalId"});
        result.ShouldNotBeNull();
    }

    [Fact]
    public async void GetProposalCountByDAOIds_Test()
    {
        _proposalIndexRepository.CountAsync(Arg.Any<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(), Arg.Any<string>())
            .Returns(new CountResponse());
        var result = await _provider.GetProposalCountByDAOIds("ChainId", "DaoId");
        result.ShouldBe(0);
    }

    [Fact]
    public async void BulkAddOrUpdateAsync_Test()
    {
        await _provider.BulkAddOrUpdateAsync(new List<ProposalIndex>());
    }

    [Fact]
    public async void GetNonFinishedProposalListAsync_Test()
    {
        _proposalIndexRepository.GetListAsync(
                Arg.Any<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
                Arg.Any<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
                Arg.Any<Expression<Func<ProposalIndex, object>>>(), Arg.Any<SortOrder>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex> { new() }));
        var result = await _provider.GetNonFinishedProposalListAsync(0, new List<ProposalStage>());
        result.ShouldNotBeNull();
    }

    [Fact]
    public async void GetNeedChangeProposalListAsync_Test()
    {
        _proposalIndexRepository.GetListAsync(
                Arg.Any<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
                Arg.Any<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
                Arg.Any<Expression<Func<ProposalIndex, object>>>(), Arg.Any<SortOrder>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex> { new() }));
        var result = await _provider.GetNeedChangeProposalListAsync(0);
        result.ShouldNotBeNull();
    }
    
    [Fact]
    public async void QueryProposalsByProposerAsync_Test()
    {
        try
        {
            await _provider.QueryProposalsByProposerAsync(null);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("");
        }
        
        try
        {
            await _provider.QueryProposalsByProposerAsync(new QueryProposalByProposerRequest());
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("");
        }
        
        _proposalIndexRepository.GetSortListAsync(
                Arg.Any<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
                Arg.Any<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
                Arg.Any<Func<SortDescriptor<ProposalIndex>, IPromise<IList<ISort>>>>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex> { new() }));
        var result = await _provider.QueryProposalsByProposerAsync(new QueryProposalByProposerRequest
        {
            ChainId = "ChainId", DaoId = "DaoId", ProposalStatus = ProposalStatus.Abstained, ProposalStage = ProposalStage.Active,
            MaxResultCount = 10, Proposer = "Propposer", SkipCount = 0
        });
        result.ShouldNotBeNull();
    }
}