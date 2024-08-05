using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Forum.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Forum;

public class ForumServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IForumService _forumService;
    private readonly ForumService _forumServiceClass;
    
    public ForumServiceTest(ITestOutputHelper output) : base(output)
    {
        _forumService = ServiceProvider.GetRequiredService<IForumService>();
        _forumServiceClass = ServiceProvider.GetRequiredService<ForumService>();
    }

    [Fact]
    public async Task LinkPreviewAsyncTest()
    {
        var previewDto = await _forumService.LinkPreviewAsync(input: new LinkPreviewInput
        {
            ProposalId = null,
            ChainId = null,
            ForumUrl = "https://www.google.com.hk/"
        });
        previewDto.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task LinkPreviewAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _forumService.LinkPreviewAsync(input: new LinkPreviewInput
            {
                ProposalId = null,
                ChainId = null,
                ForumUrl = null
            });
        });
        exception.Message.ShouldContain("Invalid input.");
    }

    [Fact]
    public async Task AnalyzePageByPuppeteerSharpAsyncTest()
    {
        string url = "https://www.google.com.hk/";
        var exception = await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(async () =>
        {
            await _forumServiceClass.AnalyzePageByPuppeteerSharpAsync(url, false);
        });
        exception.ShouldNotBeNull();
    }
}