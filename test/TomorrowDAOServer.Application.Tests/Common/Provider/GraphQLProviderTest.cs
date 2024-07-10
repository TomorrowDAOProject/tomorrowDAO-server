using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Providers;
using Xunit;

namespace TomorrowDAOServer.Common.Provider;

public class GraphQLProviderTest
{
    private readonly IGraphQLClient _graphQlClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly IGraphQlClientFactory _graphQlClientFactory;
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly IIndexerProvider _indexerProvider;
    private readonly IGraphQLProvider _provider;

    public GraphQLProviderTest()
    {
        _indexerProvider = Substitute.For<IIndexerProvider>();
        _graphQlHelper = Substitute.For<IGraphQlHelper>();
        _graphQlClientFactory = Substitute.For<IGraphQlClientFactory>();
        _logger = Substitute.For<ILogger<GraphQLProvider>>();
        _clusterClient = Substitute.For<IClusterClient>();
        _graphQlClient = Substitute.For<IGraphQLClient>();
        _provider = new GraphQLProvider(_graphQlClient, _logger, _clusterClient, _graphQlClientFactory, _graphQlHelper,
            _indexerProvider);
    }

    [Fact]
    public async void GetSyncState_Test()
    {
        _indexerProvider.GetSyncStateAsync(Arg.Any<string>()).Returns(0);
        var height = await _provider.GetIndexBlockHeightAsync("AELF");
        height.ShouldBe(0L);
    }
}