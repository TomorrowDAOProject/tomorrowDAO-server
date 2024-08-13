using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using Xunit;

namespace TomorrowDAOServer.Token;

public class TokenPriceUpdateServiceTest
{
    private readonly ILogger<TokenPriceUpdateService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IChainAppService _chainAppService;
    private readonly IOptionsMonitor<NetworkDaoOptions> _networkDaoOptions;
    private readonly ITokenService _tokenService;
    private readonly IScheduleSyncDataService _service;

    public TokenPriceUpdateServiceTest()
    {
        _logger = Substitute.For<ILogger<TokenPriceUpdateService>>();
        _graphQlProvider = Substitute.For<IGraphQLProvider>();
        _chainAppService = Substitute.For<IChainAppService>();
        _networkDaoOptions = Substitute.For<IOptionsMonitor<NetworkDaoOptions>>();
        _tokenService = Substitute.For<ITokenService>();
        _service = new TokenPriceUpdateService(_logger, _graphQlProvider, _chainAppService, _networkDaoOptions, _tokenService);
    }

    [Fact]
    public async Task SyncIndexerRecordsAsync_Test()
    {
        _networkDaoOptions.CurrentValue.Returns(new NetworkDaoOptions { PopularSymbols = new List<string> { "ELF" } });
        var result = await _service.SyncIndexerRecordsAsync("tDVW", 0, 0);
        result.ShouldBe(-1);
    }

    [Fact]
    public async Task GetChainIdsAsync_Test()
    {
        _chainAppService.GetListAsync().Returns(new[] { "tDVW" });
        var result = await _service.GetChainIdsAsync();
        result.Count.ShouldBe(1);
        result[0].ShouldBe("tDVW");
    }

    [Fact]
    public void GetBusinessType_Test()
    {
        var result = _service.GetBusinessType();
        result.ShouldBe(WorkerBusinessType.TokenPriceUpdate);
    }
}