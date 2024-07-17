using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Aws;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Grains.Grain;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Options;
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
    private readonly ITokenService _service;
    
    private readonly ILogger<TokenGrain> _logger1;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IContractProvider _contractProvider;
    private readonly IAwsS3Client _awsS3Client;

    public TokenServiceTest()
    {
        _clusterClient = Substitute.For<IClusterClient>();
        _logger = Substitute.For<ILogger<TokenService>>();
        _explorerProvider = Substitute.For<IExplorerProvider>();
        _objectMapper = Substitute.For<IObjectMapper>();
        _graphQlProvider = Substitute.For<IGraphQLProvider>();
        _service = new TokenService(_clusterClient, _logger, _explorerProvider, _objectMapper, _graphQlProvider);
    }

    [Fact]
    public async void GetTokenAsync_Test()
    {
        var grain = Substitute.For<ITokenGrain>();
        _clusterClient.GetGrain<ITokenGrain>(Arg.Any<string>())
            .Returns(grain);
        grain.GetTokenAsync(Arg.Any<TokenGrainDto>())
            .Returns(new GrainResultDto<TokenGrainDto>
            {
                Data = new TokenGrainDto{TokenName = "ELF"}, Success = true
            });
        var tokenGrain = await _service.GetTokenAsync("AELF", "ELF");
        tokenGrain.ShouldNotBeNull();
        tokenGrain.TokenName.ShouldBe("ELF");
    }
}