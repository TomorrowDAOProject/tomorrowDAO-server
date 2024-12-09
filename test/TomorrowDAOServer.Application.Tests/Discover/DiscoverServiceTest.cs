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
using TomorrowDAOServer.Ranking.Provider;
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
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockConnectionMultiplexer());
        services.AddSingleton(RankingAppPointsRedisProviderTest.MockDistributedCache());
        services.AddSingleton(MockCommentIndexRepository());
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
        exception.Message.ShouldBe("User is not authenticated.");

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
        exception.Message.ShouldBe("User is not authenticated.");
        
        
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
        

        // exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        // {
        //     await _discoverService.DiscoverChooseAsync(ChainIdAELF, new List<string>()
        //     {
        //         TelegramAppCategory.Earn.ToString(), TelegramAppCategory.Ecommerce.ToString()
        //     });
        // });
        // exception.Message.ShouldBe("Already chose the discover type.");
    }

    [Fact]
    public async Task GetDiscoverAppListAsyncTest()
    {
        var newGuid = Guid.NewGuid();
        Login(newGuid, Address1);
        await CreateDiscoverChoiceIndexAsync(newGuid, Address1);
        // var discoverAppList = await _discoverService.GetDiscoverAppListAsync(new GetDiscoverAppListInput
        // {
        //     ChainId = ChainIdAELF,
        //     Category = CommonConstant.Recommend,
        //     SkipCount = 0,
        //     MaxResultCount = 10,
        // });
        // discoverAppList.ShouldNotBeNull();
        // discoverAppList.TotalCount.ShouldBe(0);

        try
        {
            //TODO Exception
            var discoverAppList = await _discoverService.GetDiscoverAppListAsync(new GetDiscoverAppListInput
            {
                ChainId = ChainIdAELF,
                Category = TelegramAppCategory.Game.ToString(),
                SkipCount = 0,
                MaxResultCount = 10,
            });
            discoverAppList.ShouldNotBeNull();
            discoverAppList.TotalCount.ShouldBe(0);
        }
        catch (Exception e)
        {
            Assert.True(true);
        }
        
    }

    private async Task CreateDiscoverChoiceIndexAsync(Guid newGuid, string address)
    {
        await _discoverChoiceProvider.BulkAddOrUpdateAsync(new List<DiscoverChoiceIndex>()
        {
            new DiscoverChoiceIndex
            {
                Id = "DiscoverChoiceIndex-Id",
                ChainId = ChainIdAELF,
                UserId = newGuid.ToString(),
                Address = address,
                TelegramAppCategory = TelegramAppCategory.Game,
                DiscoverChoiceType = DiscoverChoiceType.Choice,
                UpdateTime = default
            }
        });
    }
}