using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Grains.Grain;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.User.Dtos;
using Xunit;

namespace TomorrowDAOServer.User.Provider;

public class UserProviderTest
{
    private readonly ILogger<UserProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IUserProvider _provider;
    private readonly IUserGrain _userGrain;
    private const string UserId = "158ff364-3264-4234-ab20-02aaada2aaad";

    public UserProviderTest()
    {
        _logger = Substitute.For<ILogger<UserProvider>>();
        _clusterClient = Substitute.For<IClusterClient>();
        _provider = new UserProvider(_logger, _clusterClient);
        _userGrain = Substitute.For<IUserGrain>();
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
        var result = await _provider.GetUserAddress(Guid.Empty, "");
        result.ShouldBe(string.Empty);
        
        result = await _provider.GetUserAddress(Guid.Empty, "chainId");
        result.ShouldBe(string.Empty);
        
        _clusterClient.GetGrain<IUserGrain>(Arg.Any<Guid>()).Returns(_userGrain);
        _userGrain.GetUser().Returns(new GrainResultDto<UserGrainDto>
        {
            Success = true, Data = new UserGrainDto{AddressInfos = new List<AddressInfo>{new(){Address = "address", ChainId = "otherChainId"}}}
        });
        result = await _provider.GetUserAddress(Guid.Parse(UserId), "chainId");
        result.ShouldBe(string.Empty);
        
        _userGrain.GetUser().Returns(new GrainResultDto<UserGrainDto>
        {
            Success = true, Data = new UserGrainDto{AddressInfos = new List<AddressInfo>{new(){Address = "address", ChainId = "chainId"}}}
        });
        result = await _provider.GetUserAddress(Guid.Parse(UserId), "chainId");
        result.ShouldBe("address");
    }

    [Fact]
    public async Task GetAndValidateUserAddress_Test()
    {
        await GetUserAddress_Test();
        var result = await _provider.GetUserAddress(Guid.Parse(UserId), "chainId");
        result.ShouldBe("address");
    }
}