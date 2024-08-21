using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Grains.Grain;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.User.Provider;

public class UserProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ILogger<UserProvider> _logger = Substitute.For<ILogger<UserProvider>>();
    private readonly IClusterClient _clusterClient = Substitute.For<IClusterClient>();
    private readonly IUserProvider _provider;
    private readonly IUserGrain _userGrain = Substitute.For<IUserGrain>();
    private const string UserId = "158ff364-3264-4234-ab20-02aaada2aaad";

    public UserProviderTest(ITestOutputHelper output) : base(output)
    {
        _provider =  ServiceProvider.GetRequiredService<UserProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(_clusterClient);
    }

    [Fact]
    public async Task GetUserAsync_Test()
    {
        var result = await _provider.GetUserAsync(Guid.Empty);
        result.ShouldBeNull();

        _clusterClient.GetGrain<IUserGrain>(Arg.Any<Guid>()).Returns(_userGrain);
        _userGrain.GetUser().Returns(new GrainResultDto<UserGrainDto>
        {
            Success = false
        });
        result = await _provider.GetUserAsync(Guid.Parse(UserId));
        result.ShouldBeNull();
        
        _userGrain.GetUser().Returns(new GrainResultDto<UserGrainDto>
        {
            Data = new UserGrainDto{UserId = Guid.Parse(UserId)}, Success = true
        });
        result = await _provider.GetUserAsync(Guid.Parse(UserId));
        result.UserId.ShouldBe(Guid.Parse(UserId));
    }

    [Fact]
    public async Task GetUserAddress_Test()
    {
        var result = await _provider.GetUserAddressAsync(Guid.Empty, "");
        result.ShouldBe(string.Empty);
        
        result = await _provider.GetUserAddressAsync(Guid.Empty, "chainId");
        result.ShouldBe(string.Empty);
        
        _clusterClient.GetGrain<IUserGrain>(Arg.Any<Guid>()).Returns(_userGrain);
        _userGrain.GetUser().Returns(new GrainResultDto<UserGrainDto>
        {
            Success = true, Data = new UserGrainDto{AddressInfos = new List<AddressInfo>{new(){Address = "address", ChainId = "otherChainId"}}}
        });
        result = await _provider.GetUserAddressAsync(Guid.Parse(UserId), "chainId");
        result.ShouldBe(string.Empty);
        
        _userGrain.GetUser().Returns(new GrainResultDto<UserGrainDto>
        {
            Success = true, Data = new UserGrainDto{AddressInfos = new List<AddressInfo>{new(){Address = "address", ChainId = "chainId"}}}
        });
        result = await _provider.GetUserAddressAsync(Guid.Parse(UserId), "chainId");
        result.ShouldBe("address");
    }

    [Fact]
    public async Task GetAndValidateUserAddress_Test()
    {
        await GetUserAddress_Test();
        var result = await _provider.GetUserAddressAsync(Guid.Parse(UserId), "chainId");
        result.ShouldBe("address");
    }

    [Fact]
    public async Task GetAndValidateUserAddressAsyncTest()
    {
        _clusterClient.GetGrain<IUserGrain>(Arg.Any<Guid>()).Returns(_userGrain);
        _userGrain.GetUser().Returns(new GrainResultDto<UserGrainDto>
        {
            Success = true, Data = new UserGrainDto{AddressInfos = new List<AddressInfo>{new(){Address = "address", ChainId = "chainId"}}}
        });
        
        var address = await _provider.GetAndValidateUserAddressAsync(Guid.Parse(UserId), "chainId");
        address.ShouldBe("address");
    }
    
    [Fact]
    public async Task GetAndValidateUserAddressAsyncTest_NotFound()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _provider.GetAndValidateUserAddressAsync(Guid.Parse(UserId), "chainId");
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("No user address found");
    }
}