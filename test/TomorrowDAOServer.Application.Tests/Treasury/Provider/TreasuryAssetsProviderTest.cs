using GraphQL;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Treasury.Dto;
using Xunit;

namespace TomorrowDAOServer.Treasury.Provider;

public class TreasuryAssetsProviderTest
{ 
    private readonly ILogger<TreasuryAssetsProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly TreasuryAssetsProvider _provider;

    public TreasuryAssetsProviderTest()
    {
        _logger = Substitute.For<ILogger<TreasuryAssetsProvider>>();
        _graphQlHelper = Substitute.For<IGraphQlHelper>();
        _provider = new TreasuryAssetsProvider(_logger, _graphQlHelper);
    }

    [Fact]
    public async void GetTreasuryAssetsAsync_Test()
    {
        _graphQlHelper.QueryAsync<IndexerCommonResult<GetTreasuryFundListResult>>(Arg.Any<GraphQLRequest>())
            .Returns(new IndexerCommonResult<GetTreasuryFundListResult>());
        var result = await _provider.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput());
        result.ShouldNotBeNull();
    }
    
    [Fact]
    public async void GetAllTreasuryAssetsAsync_Test()
    {
        _graphQlHelper.QueryAsync<IndexerCommonResult<GetTreasuryFundListResult>>(Arg.Any<GraphQLRequest>())
            .Returns(new IndexerCommonResult<GetTreasuryFundListResult>());
        var result = await _provider.GetAllTreasuryAssetsAsync(new GetAllTreasuryAssetsInput());
        result.ShouldNotBeNull();
    }
}