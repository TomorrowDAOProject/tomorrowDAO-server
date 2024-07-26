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
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Grains.Grain;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Providers;
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
        _service = new TokenService(_clusterClient, _logger, _explorerProvider, _objectMapper, _graphQlProvider);
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
        var result = await _service.GetTokenAsync("chainId", "symbol");
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTokenByExplorerAsync_Test()
    {
        await GetTokenAsync_Test();
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<ExplorerTokenInfoRequest>())
            .Returns(new ExplorerTokenInfoResponse());
        _objectMapper.Map<ExplorerTokenInfoResponse, TokenDto>(Arg.Any<ExplorerTokenInfoResponse>())
            .Returns(new TokenDto());
        var result = await _service.GetTokenByExplorerAsync("chainId", "symbol");
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTvlAsync_Test()
    {
        _graphQlProvider.GetDAOAmountAsync(Arg.Any<string>()).Returns(new List<DAOAmount>
        {
            new() { Amount = 100000000, GovernanceToken = "ELF" }
        });
        _explorerProvider.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(new TokenInfoDto
        {
            Symbol = "ELF", Decimals = "8"
        });
        _clusterClient.GetGrain<ITokenExchangeGrain>(Arg.Any<string>()).Returns(_exchangeGrain);
        _exchangeGrain.GetAsync().Returns(new Dictionary<string, TokenExchangeDto>
        {
            { "OKX", new TokenExchangeDto { Exchange = (decimal)0.4 } }
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
        _exchangeGrain.GetAsync().Returns(new Dictionary<string, TokenExchangeDto>());
        result = await _service.GetTokenPriceAsync("", "");
        result.Price.ShouldBe(0);
    }
    
    
}