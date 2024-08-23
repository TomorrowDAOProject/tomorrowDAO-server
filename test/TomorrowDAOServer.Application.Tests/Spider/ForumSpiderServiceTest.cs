using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Spider.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Spider;

public class ForumSpiderServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IForumSpiderService _forumSpiderService;
    private readonly ForumSpiderService _forumSpiderServiceClass;
    
    public ForumSpiderServiceTest(ITestOutputHelper output) : base(output)
    {
        _forumSpiderService = ServiceProvider.GetRequiredService<IForumSpiderService>();
        _forumSpiderServiceClass = ServiceProvider.GetRequiredService<ForumSpiderService>();
    }

    [Fact]
    public async Task LinkPreviewAsyncTest()
    {
        var previewDto = await _forumSpiderService.LinkPreviewAsync(input: new LinkPreviewInput
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
            await _forumSpiderService.LinkPreviewAsync(input: new LinkPreviewInput
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
            await _forumSpiderServiceClass.AnalyzePageByPuppeteerSharpAsync(url, false);
        });
        exception.ShouldNotBeNull();
    }
}