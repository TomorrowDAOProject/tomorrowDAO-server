using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Dtos.Indexer;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Providers;

public interface IIndexerProvider
{
    Task<long> GetSyncStateAsync(string chainId);
}

public static class IndexerApi
{
    public static readonly ApiInfo SyncState = new(HttpMethod.Get, "/api/apps/sync-state/tomorrowdao_indexer");
}

public class IndexerProvider : IIndexerProvider, ISingletonDependency
{
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<IndexerOptions> _indexerOptions;
    private readonly IGraphQLClient _graphQlClient;
    
    public IndexerProvider(IHttpProvider httpProvider, IOptionsMonitor<IndexerOptions> indexerOptions, IGraphQLClient graphQlClient)
    {
        _httpProvider = httpProvider;
        _indexerOptions = indexerOptions;
        _graphQlClient = graphQlClient;
    }

    public async Task<long> GetSyncStateAsync(string chainId)
    {
        if (_indexerOptions.CurrentValue.UseNewIndexer)
        {
            _indexerOptions.CurrentValue.BaseUrl.TryGetValue(chainId, out var domain);
            var resp = await _httpProvider.InvokeAsync<IndexerSyncStateResponse>(domain, IndexerApi.SyncState,
                withInfoLog: false, withDebugLog: false);
            var syncDetail = resp?.CurrentVersion?.Items?.Single(x => x.ChainId == chainId);
            return syncDetail?.LongestChainHeight ?? 0L;
        }
        
        var graphQlResponse = await _graphQlClient.SendQueryAsync<ConfirmedBlockHeightRecord>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$filterType:BlockFilterType!) {
                    syncState(input: {chainId:$chainId,filterType:$filterType}){
                        confirmedBlockHeight}
                    }",
            Variables = new
            {
                chainId,
                filterType = BlockFilterType.LOG_EVENT
            }
        });
        return graphQlResponse.Data.SyncState.ConfirmedBlockHeight;
    }
}