using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Discover.Dto;
using TomorrowDAOServer.Discover.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Discover;

public partial class DiscoverServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IDiscoverService _discoverService;
    private readonly IDiscoverChoiceProvider _discoverChoiceProvider;
    
    public DiscoverServiceTest(ITestOutputHelper output) : base(output)
    {
        _discoverService = Application.ServiceProvider.GetRequiredService<IDiscoverService>();
        _discoverChoiceProvider = Application.ServiceProvider.GetRequiredService<IDiscoverChoiceProvider>();
    }

    [Fact]
    public async Task DiscoverViewedAsyncTest()
    {
        Login(Guid.Empty);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _discoverService.DiscoverViewedAsync(ChainIdAELF);
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("No user address found");

        Login(Guid.NewGuid(), Address1);
        var discoverViewed = await _discoverService.DiscoverViewedAsync(ChainIdAELF);
        discoverViewed.ShouldBeFalse();

        discoverViewed = await _discoverService.DiscoverViewedAsync(ChainIdAELF);
        discoverViewed.ShouldBeTrue();
    }

    [Fact]
    public async Task DiscoverChooseAsync()
    {
        Login(Guid.Empty);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
            {
                "abc", "def"
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("No user address found");
        
        
        Login(Guid.NewGuid(), Address1);
        var discoverChoose = false;
        var throwException = false;
        try
        {
            discoverChoose = await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
            {
                TelegramAppCategory.Earn.ToString(), TelegramAppCategory.Ecommerce.ToString()
            });
        }
        catch (Exception e)
        {
            if (e is not UserFriendlyException ||
                !e.Message.Contains("Already chose the discover type"))
            {
                throw;
            }
            throwException = true;
        }

        if (throwException)
        {
            discoverChoose.ShouldBeFalse();
        }
        else
        {
            discoverChoose.ShouldBeTrue();
        }
        

        exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
            {
                TelegramAppCategory.Earn.ToString(), TelegramAppCategory.Ecommerce.ToString()
            });
        });
        exception.Message.ShouldBe("Already chose the discover type.");
    }

    [Fact]
    public async Task GetDiscoverAppListAsyncTest()
    {
        Login(Guid.NewGuid(), Address1);
        await CreateDiscoverChoiceIndexAsync();
        var discoverAppList = await _discoverService.GetDiscoverAppListAsync(new GetDiscoverAppListInput
        {
            ChainId = ChainIdAELF,
            Category = CommonConstant.Recommend,
            SkipCount = 0,
            MaxResultCount = 10,
        });
        discoverAppList.ShouldNotBeNull();
        discoverAppList.TotalCount.ShouldBe(0);
        
        discoverAppList = await _discoverService.GetDiscoverAppListAsync(new GetDiscoverAppListInput
        {
            ChainId = ChainIdAELF,
            Category = TelegramAppCategory.Game.ToString(),
            SkipCount = 0,
            MaxResultCount = 10,
        });
        discoverAppList.ShouldNotBeNull();
        discoverAppList.TotalCount.ShouldBe(0);
        
    }

    private async Task CreateDiscoverChoiceIndexAsync()
    {
        await _discoverChoiceProvider.BulkAddOrUpdateAsync(new List<DiscoverChoiceIndex>()
        {
            new DiscoverChoiceIndex
            {
                Id = "DiscoverChoiceIndex-Id",
                ChainId = ChainIdAELF,
                Address = Address1,
                TelegramAppCategory = TelegramAppCategory.Game,
                DiscoverChoiceType = DiscoverChoiceType.Choice,
                UpdateTime = default
            }
        });
    }
}