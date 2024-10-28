using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Grains.Grain.Discussion;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Discussion.Provider;

public class DiscussionProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IDiscussionProvider _discussionProvider;
    private readonly IClusterClient _clusterClient;

    public DiscussionProviderTest(ITestOutputHelper output) : base(output)
    {
        _discussionProvider = Application.ServiceProvider.GetRequiredService<IDiscussionProvider>();
        _clusterClient = Application.ServiceProvider.GetRequiredService<IClusterClient>();
    }
    
    [Fact]
    public async void GetCommentCountAsync_Test()
    {
        var commentCountGrain = _clusterClient.GetGrain<ICommentCountGrain>(ProposalId1);
        var nextCount = await commentCountGrain.GetNextCount();
        nextCount.ShouldBe(1);
        var count = await _discussionProvider.GetCommentCountAsync(ProposalId1);
        count.ShouldBe(2);
    }

    [Fact]
    public async Task NewCommentAsync_Test()
    {
        await _discussionProvider.NewCommentAsync(new CommentIndex
        {
            Id = "Id",
            ChainId = ChainIdAELF,
            DAOId = DAOId,
            ProposalId = ProposalId1,
            Commenter = null,
            Deleter = null,
            Comment = null,
            ParentId = DAOId,
            CommentStatus = CommentStatusEnum.Normal,
            CreateTime = 2,
            ModificationTime = 0
        });
    }

    [Fact]
    public async Task CountCommentListAsyncTest()
    {
        await NewCommentAsync_Test();
        var (count, commentIndices) = await _discussionProvider.GetCommentListAsync(new GetCommentListInput
        {
            ChainId = ChainIdAELF,
            ProposalId = ProposalId1,
            Alias = null,
            ParentId = DAOId,
            SkipCount = 0,
            MaxResultCount = 20,
            SkipId = null
        });
        count.ShouldBe(1);
        commentIndices.Count.ShouldBe(1);
    }
    
    [Fact]
    public async void GetCommentListAsync_Test()
    {
        await _discussionProvider.GetCommentListAsync(new GetCommentListInput
        {
            ChainId = "chainId", ProposalId = "proposalId"
        });
    }
    
    [Fact]
    public async void GetCommentAsync_Test()
    {
        await NewCommentAsync_Test();
        var commentIndex = await _discussionProvider.GetCommentAsync("Id");
        commentIndex.ShouldNotBeNull();
        commentIndex.ProposalId.ShouldBe(ProposalId1);
    }
    
    [Fact]
    public async void GetAllCommentsByProposalIdAsync_Test()
    {
        await NewCommentAsync_Test();
        var (count, commentIndices) = await _discussionProvider.GetAllCommentsByProposalIdAsync("chainId", "proposalId");
        count.ShouldBe(0);
        commentIndices.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetEarlierAsyncTest()
    {
        await NewCommentAsync_Test();
        var (count, commentIndices) = await _discussionProvider.GetEarlierAsync("NotId", ProposalId1, 2, 10);
        count.ShouldBe(1);
        commentIndices.Count.ShouldBe(1);
    }
}