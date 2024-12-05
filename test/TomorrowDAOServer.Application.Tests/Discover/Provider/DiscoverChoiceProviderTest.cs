using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Discover.Provider;

public partial class DiscoverChoiceProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IDiscoverChoiceProvider _discoverChoiceProvider;
    private readonly IClusterClient _clusterClient;

    public DiscoverChoiceProviderTest(ITestOutputHelper output) : base(output)
    {
        _discoverChoiceProvider = Application.ServiceProvider.GetRequiredService<IDiscoverChoiceProvider>();
        _clusterClient = Application.ServiceProvider.GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task DiscoverViewedAsyncTest()
    {
        // var discoverViewedGrain = _clusterClient.GetGrain<IDiscoverViewedGrain>(GuidHelper.GenerateGrainId(ChainIdAELF, Address1));
        // discoverViewedGrain.AsReference<>()
        var discoverViewed = await _discoverChoiceProvider.DiscoverViewedAsync(ChainIdAELF, Address1, "");
        discoverViewed.ShouldBeFalse();
        
        discoverViewed = await _discoverChoiceProvider.DiscoverViewedAsync(ChainIdAELF, Address1, "");
        discoverViewed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetExistByAddressAndDiscoverTypeAsyncTest()
    {
        var exist = await _discoverChoiceProvider.GetExistByAddressAndUserIdAndDiscoverTypeAsync(ChainIdAELF, "", Address1,
            DiscoverChoiceType.Choice);
        if (!exist)
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
                    UpdateTime = DateTime.Now
                }
            });
        }
        
        exist = await _discoverChoiceProvider.GetExistByAddressAndUserIdAndDiscoverTypeAsync(ChainIdAELF, "", Address1,
            DiscoverChoiceType.Choice);
        exist.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByAddressAsyncTest()
    {
        var choiceIndices = await _discoverChoiceProvider.GetByAddressOrUserIdAsync(ChainIdAELF, Address1, "");
        if (choiceIndices.IsNullOrEmpty())
        {
            await _discoverChoiceProvider.BulkAddOrUpdateAsync(new List<DiscoverChoiceIndex>()
            {
                new DiscoverChoiceIndex
                {
                    Id = "DiscoverChoiceIndex-Id2",
                    ChainId = ChainIdAELF,
                    Address = Address2,
                    TelegramAppCategory = TelegramAppCategory.Game,
                    DiscoverChoiceType = DiscoverChoiceType.Choice,
                    UpdateTime = DateTime.Now
                }
            });
        
            choiceIndices = await _discoverChoiceProvider.GetByAddressOrUserIdAsync(ChainIdAELF, Address2, "");
            choiceIndices.ShouldNotBeEmpty();
            choiceIndices[0].Address.ShouldBe(Address2);
        }
        else
        {
            choiceIndices[0].Address.ShouldBe(Address1);
        }
    }
        
}