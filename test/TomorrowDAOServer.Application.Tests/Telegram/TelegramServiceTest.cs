using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Telegram.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Telegram;

public partial class TelegramServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITelegramService _telegramService;

    public TelegramServiceTest(ITestOutputHelper output) : base(output)
    {
        _telegramService = ServiceProvider.GetRequiredService<ITelegramService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockTelegramOptions());
    }

    [Fact]
    public async Task SaveTelegramAppsAsyncTest()
    {
        await _telegramService.SaveTelegramAppsAsync(new List<TelegramAppDto>());
        
        await _telegramService.SaveTelegramAppsAsync(new List<TelegramAppDto>
        {
            new TelegramAppDto
            {
                Id = Guid.NewGuid().ToString(),
                Alias = null,
                Title = null,
                Icon = null,
                Description = null,
                EditorChoice = false
            }
        });
    }

    [Fact]
    public async Task SaveTelegramAppAsyncTest()
    {
        await _telegramService.SaveTelegramAppAsync(new TelegramAppDto(), null);
        
        Login(Guid.NewGuid(),Address2);
        await _telegramService.SaveTelegramAppAsync(new TelegramAppDto
        {
            Id = Guid.NewGuid().ToString(),
            Alias = "Alias",
            Title = "Title",
            Icon = "Icon",
            Description = "Description",
            EditorChoice = false
        }, ChainIdAELF);
    }
    
    [Fact]
    public async Task SaveTelegramAppAsyncTest_AccessDenied()
    {
        
        Login(Guid.NewGuid(), Address1);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramService.SaveTelegramAppAsync(new TelegramAppDto
            {
                Id = Guid.NewGuid().ToString(),
                Alias = "Alias",
                Title = "Title",
                Icon = "Icon",
                Description = "Description",
                EditorChoice = false
            }, ChainIdAELF);
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Access denied.");
    }
}