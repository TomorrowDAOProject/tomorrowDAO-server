using System;
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
}