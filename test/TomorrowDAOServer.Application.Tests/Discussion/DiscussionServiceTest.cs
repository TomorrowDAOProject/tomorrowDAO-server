using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Discussion.Provider;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Discussion;

public partial class DiscussionServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IDiscussionService _discussionService;
    private readonly IDiscussionProvider _discussionProvider;
    
    public DiscussionServiceTest(ITestOutputHelper output) : base(output)
    {
        _discussionService = Application.ServiceProvider.GetRequiredService<IDiscussionService>();
        _discussionProvider = Application.ServiceProvider.GetRequiredService<IDiscussionProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockProposalProvider());
        services.AddSingleton(MockDaoProvider());
    }

    [Fact]
    public async Task NewCommentAsyncTest()
    {
        await CreateNewCommentAsync();
        
        Login(Guid.NewGuid(), Address2);
        var newCommentResult = await _discussionService.NewCommentAsync(new NewCommentInput
        {
            ChainId = ChainIdAELF,
            ProposalId = ProposalId1,
            Alias = null,
            ParentId = "Id",
            Comment = Address1
        });
        newCommentResult.ShouldNotBeNull();
        newCommentResult.Reason.ShouldBe("Invalid proposalId: not existed.");
        
        Login(Guid.NewGuid(), Address1);
        newCommentResult = await _discussionService.NewCommentAsync(new NewCommentInput
        {
            ChainId = ChainIdAELF,
            ProposalId = ProposalId1,
            Alias = null,
            ParentId = "Id",
            Comment = Address1
        });
        newCommentResult.ShouldNotBeNull();
        newCommentResult.Reason.ShouldBe("Invalid proposalId: not existed.");
    }

    [Fact]
    public async Task GetCommentListAsync()
    {
        await CreateNewCommentAsync();
        var commentList = await _discussionService.GetCommentListAsync(new GetCommentListInput
        {
            ChainId = ChainIdAELF,
            ProposalId = ProposalId1,
            Alias = null,
            ParentId = "Id",
            SkipCount = 0,
            MaxResultCount = 10,
            SkipId = "Ids"
        });
        commentList.ShouldNotBeNull();
        
        commentList = await _discussionService.GetCommentListAsync(new GetCommentListInput
        {
            ChainId = ChainIdAELF,
            ProposalId = null,
            Alias = "Alias",
            ParentId = "Id",
            SkipCount = 0,
            MaxResultCount = 10,
            SkipId = "Ids"
        });
        commentList.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCommentBuildingAsyncTest()
    {
        await CreateNewCommentAsync();
        var commentBuilding = await _discussionService.GetCommentBuildingAsync(new GetCommentBuildingInput
        {
            ChainId = ChainIdAELF,
            ProposalId = ProposalId1
        });
        commentBuilding.ShouldNotBeNull();
        commentBuilding.CommentBuilding.Id.ShouldBe("root");
    }
    
    private async Task CreateNewCommentAsync()
    {
        await _discussionProvider.NewCommentAsync(new CommentIndex
        {
            Id = "Id",
            ChainId = ChainIdAELF,
            DAOId = DAOId,
            ProposalId = ProposalId1,
            Commenter = Address1,
            Deleter = null,
            Comment = Address1,
            ParentId = DAOId,
            CommentStatus = CommentStatusEnum.Normal,
            CreateTime = 2,
            ModificationTime = 0
        });
    }
}