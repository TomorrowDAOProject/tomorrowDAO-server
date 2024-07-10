using System.Collections.Generic;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Dtos.Indexer;
using TomorrowDAOServer.Options;
using Xunit;

namespace TomorrowDAOServer.Providers;

public class IndexerProviderTest
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<IndexerOptions> _indexerOptions;
    private readonly IGraphQLClient _graphQlClient;
    private readonly IIndexerProvider _provider;

    public IndexerProviderTest()
    {
        _graphQlClient = Substitute.For<IGraphQLClient>();
        _indexerOptions = Substitute.For<IOptionsMonitor<IndexerOptions>>();
        _httpProvider = Substitute.For<IHttpProvider>();
        _provider = new IndexerProvider(_httpProvider, _indexerOptions, _graphQlClient);
    }

    [Fact]
    public async void GetSyncStateAsync_Test()
    {
        _indexerOptions.CurrentValue
            .Returns(new IndexerOptions
            {
                UseNewIndexer = false,
                BaseUrl = new Dictionary<string, string>()
            });
        _graphQlClient.SendQueryAsync<ConfirmedBlockHeightRecord>(Arg.Any<GraphQLRequest>())
            .Returns(new GraphQLResponse<ConfirmedBlockHeightRecord>
            {
                Data = new ConfirmedBlockHeightRecord { SyncState = new SyncState { ConfirmedBlockHeight = 0L } }
            });
        var height = await _provider.GetSyncStateAsync("AELF");
        height.ShouldBe(0L);
        
        
        _indexerOptions.CurrentValue
            .Returns(new IndexerOptions
            {
                UseNewIndexer = true,
                BaseUrl = new Dictionary<string, string>
                {
                    {"AELF", "url"}
                }
            });
        _httpProvider.InvokeAsync<IndexerSyncStateResponse>(Arg.Any<string>(), Arg.Any<ApiInfo>(),
                Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
                Arg.Any<JsonSerializerSettings>(), Arg.Any<int>(),
                Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(new IndexerSyncStateResponse
            {
                CurrentVersion = new VersionDetail
                {
                    Version = "",
                    Items = new List<SyncDetail>
                    {
                        new()
                        {
                            ChainId = "AELF", LongestChainHeight = 0L
                        }
                    }
                }
            });
        height = await _provider.GetSyncStateAsync("AELF");
        height.ShouldBe(0L);
    }
}