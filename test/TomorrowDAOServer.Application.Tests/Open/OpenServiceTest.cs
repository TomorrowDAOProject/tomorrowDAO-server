using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Open.Dto;
using TomorrowDAOServer.Telegram.Provider;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Open;

public class OpenServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IOpenService _openService;
    private readonly ITelegramUserInfoProvider _telegramUserInfoProvider;
    
    public OpenServiceTest(ITestOutputHelper output) : base(output)
    {
        _openService = Application.ServiceProvider.GetRequiredService<IOpenService>();
        _telegramUserInfoProvider = Application.ServiceProvider.GetRequiredService<ITelegramUserInfoProvider>();
    }

    [Fact]
    public async Task GetGalxeTaskStatusAsyncTest()
    {
        var statusDto = await _openService.GetGalxeTaskStatusAsync(new GetGalxeTaskStatusInput());
        statusDto.ShouldNotBeNull();
        statusDto.TelegramId.ShouldBeNull();
        
        statusDto = await _openService.GetGalxeTaskStatusAsync(new GetGalxeTaskStatusInput
        {
            Address = Address1
        });
        statusDto.ShouldNotBeNull();
        statusDto.Address.ShouldBe(Address1);
        statusDto.TelegramId.ShouldBeNull();
        
        statusDto = await _openService.GetGalxeTaskStatusAsync(new GetGalxeTaskStatusInput
        {
            TelegramId = "1234567"
        });
        statusDto.ShouldNotBeNull();
        statusDto.TelegramId.ShouldBe("1234567");
        statusDto.Address.ShouldBeNull();

        await _telegramUserInfoProvider.AddOrUpdateAsync(new TelegramUserInfoIndex
        {
            Id = "id",
            ChainId = ChainIdAELF,
            Address = Address1,
            Icon = "icon",
            FirstName = "FirstName",
            LastName = "LastName",
            UserName = "UserName",
            TelegramId = "1234567",
            UpdateTime = DateTime.UtcNow
        });
        
        statusDto = await _openService.GetGalxeTaskStatusAsync(new GetGalxeTaskStatusInput
        {
            TelegramId = "1234567"
        });
        statusDto.ShouldNotBeNull();
        statusDto.TelegramId.ShouldBe("1234567");
        statusDto.VoteCount.ShouldBe(0);
    }
}