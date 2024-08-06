using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Grains.Grain;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.ThirdPart.Exchange;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace TomorrowDAOServer.Token;

public class TokenServiceTest
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TokenService> _logger;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IGraphQLProvider _graphQlProvider;
    private IEnumerable<IExchangeProvider> _exchangeProviders;
    private readonly IOptionsMonitor<NetWorkReflectionOptions> _netWorkReflectionOption;
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly TokenService _service;
    private readonly ITokenGrain _tokenGrain;
    private readonly ITokenExchangeGrain _exchangeGrain;

    public TokenServiceTest()
    {
        _clusterClient = Substitute.For<IClusterClient>();
        _logger = Substitute.For<ILogger<TokenService>>();
        _explorerProvider = Substitute.For<IExplorerProvider>();
        _graphQlProvider = Substitute.For<IGraphQLProvider>();
        _objectMapper = Substitute.For<IObjectMapper>();
        _exchangeProviders = Substitute.For<IEnumerable<IExchangeProvider>>();
        _netWorkReflectionOption = Substitute.For<IOptionsMonitor<NetWorkReflectionOptions>>();
        _exchangeOptions = Substitute.For<IOptionsMonitor<ExchangeOptions>>();
        _service = new TokenService(_clusterClient, _logger, _explorerProvider, _objectMapper, _graphQlProvider, 
            _exchangeProviders, _netWorkReflectionOption, _exchangeOptions);
        _tokenGrain = Substitute.For<ITokenGrain>();
        _exchangeGrain = Substitute.For<ITokenExchangeGrain>();
    }

    [Fact]
    public async Task GetTokenAsync_Test()
    {
        _clusterClient.GetGrain<ITokenGrain>(Arg.Any<string>()).Returns(_tokenGrain);
        _tokenGrain.GetTokenAsync(Arg.Any<TokenGrainDto>()).Returns(new GrainResultDto<TokenGrainDto>
        {
            Success = true, Data = new TokenGrainDto()
        });
        var result = await _service.GetTokenInfoAsync("chainId", "symbol");
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTvlAsync_Test()
    {
        _graphQlProvider.GetDAOAmountAsync(Arg.Any<string>()).Returns(new List<DAOAmount>
        {
            new() { Amount = 100000000, GovernanceToken = "ELF" }
        });
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<ExplorerTokenInfoRequest>()).Returns(new ExplorerTokenInfoResponse
        {
            Symbol = "ELF", Decimals = "8"
        });
        _clusterClient.GetGrain<ITokenExchangeGrain>(Arg.Any<string>()).Returns(_exchangeGrain);
        _exchangeGrain.GetAsync().Returns(new TokenExchangeGrainDto
        {
            LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds(), 
            ExpireTime =  DateTime.UtcNow.AddDays(1).ToUtcMilliSeconds(),
            ExchangeInfos = new Dictionary<string, TokenExchangeDto>
            {
                {"OKX", new TokenExchangeDto{ Exchange = (decimal)0.4 }}
            }
        });
        var result = await _service.GetTvlAsync("chainId");
        result.ShouldBe(0.40000000000000002);
    }

    [Fact]
    public async Task GetTokenPriceAsync_Test()
    {
        var result = await _service.GetTokenPriceAsync("", "");
        result.Price.ShouldBe(0);
        
        _clusterClient.GetGrain<ITokenExchangeGrain>(Arg.Any<string>()).Returns(_exchangeGrain);
        result = await _service.GetTokenPriceAsync("", "");
        result.Price.ShouldBe(0);
    }
    
    
}