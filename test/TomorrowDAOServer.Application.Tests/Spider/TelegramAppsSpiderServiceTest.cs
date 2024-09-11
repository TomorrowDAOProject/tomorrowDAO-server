using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Telegram.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Spider;

public partial class TelegramAppsSpiderServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITelegramAppsSpiderService _telegramAppsSpiderService;
    
    private readonly string _url = "https://tappscenter.org/api/entities/applications";

    public TelegramAppsSpiderServiceTest(ITestOutputHelper output) : base(output)
    {
        _telegramAppsSpiderService = ServiceProvider.GetRequiredService<ITelegramAppsSpiderService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockTelegramOptions());
    }

    [Fact]
    public async Task LoadTelegramAppsAsyncTest()
    {
        Login(Guid.NewGuid(), Address2);
        
        var result = await _telegramAppsSpiderService.LoadTelegramAppsAsync(new LoadTelegramAppsInput
        {
            ChainId = ChainIdAELF,
            Url = "https://www.tapps.center/",
            ContentType = ContentType.Body
        });
        result.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task LoadTelegramAppsAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAppsSpiderService.LoadTelegramAppsAsync(new LoadTelegramAppsInput());
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Invalid input.");
    }
    
    [Fact]
    public async Task LoadTelegramAppsAsyncTest_AccessDenied()
    {
        Login(Guid.NewGuid());
        
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAppsSpiderService.LoadTelegramAppsAsync(new LoadTelegramAppsInput
            {
                ChainId = ChainIdAELF,
                Url = "http://123.com",
                ContentType = ContentType.Body
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Access denied.");
    }
    
    [Fact]
    public async Task LoadTelegramAppsAsyncTest_Unsupported()
    {
        Login(Guid.NewGuid(), Address2);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAppsSpiderService.LoadTelegramAppsAsync(new LoadTelegramAppsInput
            {
                ChainId = ChainIdAELF,
                Url = "https://www.tapps.center/",
                ContentType = 0
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Unsupported ContentType.");
        
        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAppsSpiderService.LoadTelegramAppsAsync(new LoadTelegramAppsInput
            {
                ChainId = ChainIdAELF,
                Url = "https://www.tapps.center/",
                ContentType = ContentType.Script
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Analyze script is not supported yet.");
    }
    
    [Fact]
    public async Task LoadTelegramAppsDetailAsync_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAppsSpiderService.LoadTelegramAppsDetailAsync(new LoadTelegramAppsDetailInput());
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Invalid input.");
    }
    
    [Fact]
    public async Task LoadTelegramAppsDetailAsync_Empty()
    {
        var result = await _telegramAppsSpiderService.LoadTelegramAppsDetailAsync(new LoadTelegramAppsDetailInput
        {
            ChainId = ChainIdAELF,
            Url = "http://1234567890.nf",
            Header = null,
            Apps = null
        });
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
    
    [Fact]
    public async Task LoadTelegramAppsDetailAsync_AccessDenied()
    {
        Login(Guid.NewGuid());
        
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramAppsSpiderService.LoadTelegramAppsDetailAsync(new LoadTelegramAppsDetailInput
            {
                ChainId = ChainIdAELF,
                Url = "http://1234567890.nf",
                Header = null,
                Apps = new Dictionary<string, string>() {{"AA","aa"}}
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Access denied.");
    }
    
    [Fact]
    public async Task LoadTelegramAppsDetailAsync()
    {
        MockTelegramUrlRequest();
        Login(Guid.NewGuid(), Address2);
        
        var result = await _telegramAppsSpiderService.LoadTelegramAppsDetailAsync(new LoadTelegramAppsDetailInput
        {
            ChainId = ChainIdAELF,
            Url = _url,
            Header = new Dictionary<string, string>(),
            Apps = new Dictionary<string, string>() {{"AA","aa"}}
        });
        result.ShouldNotBeNull();
        result.FirstOrDefault().Key.ShouldBe("AA");
    }
}