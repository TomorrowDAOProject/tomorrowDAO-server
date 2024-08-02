using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Grains.Grain.Discussion;
using Xunit;

namespace TomorrowDAOServer.Discussion.Provider;

public class DiscussionProviderTest
{
    private readonly INESTRepository<CommentIndex, string> _commentIndexRepository;
    private readonly ILogger<DiscussionProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDiscussionProvider _provider;

    private readonly ICommentCountGrain _commentCountGrain;

    public DiscussionProviderTest()
    {
        _commentIndexRepository = Substitute.For<INESTRepository<CommentIndex, string>>();
        _logger = Substitute.For<ILogger<DiscussionProvider>>();
        _clusterClient = Substitute.For<IClusterClient>();
        _provider = new DiscussionProvider(_logger, _commentIndexRepository, _clusterClient);
        
        _commentCountGrain = Substitute.For<ICommentCountGrain>();
    }

    [Fact]
    public async void GetCommentCountAsync_Test()
    {
        _clusterClient.GetGrain<ICommentCountGrain>(Arg.Any<string>()).Returns(_commentCountGrain);
        _commentCountGrain.GetNextCount().Returns(0);
        var count = await _provider.GetCommentCountAsync("proposalId");
        count.ShouldBe(0);
    }

    [Fact]
    public async void NewCommentAsync_Test()
    {
        await _provider.NewCommentAsync(new CommentIndex());
    }
    
    [Fact]
    public async void GetCommentListAsync_Test()
    {
        await _provider.GetCommentListAsync(new GetCommentListInput
        {
            ChainId = "chainId", ProposalId = "proposalId"
        });
    }
    
    [Fact]
    public async void GetCommentAsync_Test()
    {
        await _provider.GetCommentAsync("parentId");
    }
    
    [Fact]
    public async void GetAllCommentsByProposalIdAsync_Test()
    {
        await _provider.GetAllCommentsByProposalIdAsync("chainId", "proposalId");
    }
}