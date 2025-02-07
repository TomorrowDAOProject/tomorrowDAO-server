using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Providers;

public partial class PortkeyProviderTest : TomorrowDaoServerApplicationTestBase
{
    private IPortkeyProvider _portkeyProvider;
    
    public PortkeyProviderTest(ITestOutputHelper output) : base(output)
    {
        _portkeyProvider = ServiceProvider.GetRequiredService<IPortkeyProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockHttpProvider());
    }

    [Fact]
    public async Task GetShortLinkAsyncTest()
    {
        MockShortLinkHttpRequest();
        var (shortLinkCode, inviteCode) = await _portkeyProvider.GetShortLinkAsync(ChainIdAELF, ELF);
        shortLinkCode.ShouldBe("ShortLinkCode");
        inviteCode.ShouldBe("InviteCode");
    }

    [Fact]
    public async Task GetSyncReferralListAsyncTest()
    {
        var exception = await Assert.ThrowsAsync<GraphQLHttpRequestException>(async () =>
        {
            await _portkeyProvider.GetSyncReferralListAsync("CreateAccount", 1712640000L, 1712650000L, 0, 10);
        });
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetReferralCodeCaHashAsyncTest()
    {
        var referralCodeCaHash = await _portkeyProvider.GetReferralCodeCaHashAsync(new List<string>() { "123"});
        referralCodeCaHash.ShouldNotBeNull();
        referralCodeCaHash.ShouldNotBeEmpty();
        referralCodeCaHash.Count.ShouldBe(1);
        referralCodeCaHash.First().CaHash.ShouldBe("CaHash");
    }

    [Fact]
    public async Task GetCaHolderTransactionAsyncTest()
    {
        var exception = await Assert.ThrowsAsync<GraphQLHttpRequestException>(async () =>
        {
            await _portkeyProvider.GetCaHolderTransactionAsync(ChainIdtDVW, Address1);
        });
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetHolderInfosAsyncTest()
    {
        var exception = await Assert.ThrowsAsync<GraphQLHttpRequestException>(async () =>
        {
            await _portkeyProvider.GetHolderInfosAsync(ChainIdtDVW);
        });
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetCaHolderInfoAsyncTest()
    {
        var holderInfoIndexerDto = await _portkeyProvider.GetCaHolderInfoAsync(new List<string>(){Address1}, "CaHash");
        holderInfoIndexerDto.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetGuardianIdentifiersAsyncTest()
    {
        var identifierList = await _portkeyProvider.GetGuardianIdentifiersAsync(ChainIdAELF, "ThrowException");
        identifierList.ShouldNotBeNull();
        identifierList.Guardians.ShouldBeNull();
        
        identifierList = await _portkeyProvider.GetGuardianIdentifiersAsync(ChainIdAELF, "CaHash");
        identifierList.ShouldNotBeNull();
        identifierList.Guardians.ShouldNotBeEmpty();
        identifierList.Guardians.Count.ShouldBe(1);
    }
}