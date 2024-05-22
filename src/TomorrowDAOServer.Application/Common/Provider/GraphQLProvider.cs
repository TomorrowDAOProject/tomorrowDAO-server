using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using TomorrowDAOServer.Grains.Grain.Token;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Common.Provider;

public interface IGraphQLProvider
{
    public Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol);
    public Task SetTokenInfoAsync(TokenInfoDto tokenInfo);
    public Task<List<string>> GetBPAsync(string chainId);
    public Task<BpInfoDto> GetBPWithRoundAsync(string chainId);
    public Task SetBPAsync(string chainId, List<string> addressList, long round);
    public Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType);
    public Task SetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType, long height);
    public Task<long> GetIndexBlockHeightAsync(string chainId);
    public Task<Dictionary<string, long>> GetHoldersAsync(List<string> symbols, string chainId, int skipCount, int maxResultCount);
}

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly IGraphQLClient _graphQlClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly IGraphQlClientFactory _graphQlClientFactory;

    public GraphQLProvider(IGraphQLClient graphQlClient, ILogger<GraphQLProvider> logger,
        IClusterClient clusterClient, IGraphQlClientFactory graphQlClientFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _graphQlClientFactory = graphQlClientFactory;
        _graphQlClient = graphQlClient;
    }

    public async Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IExplorerTokenGrain>(GuidHelper.GenerateGrainId(chainId, symbol));
            return await grain.GetTokenInfoAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTokenInfoAsync Exception chainId {chainId} symbol {symbol}", chainId, symbol);
            return new TokenInfoDto();
        }
    }

    public async Task SetTokenInfoAsync(TokenInfoDto tokenInfo)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IExplorerTokenGrain>(GuidHelper.GenerateGrainId(tokenInfo.ChainId, tokenInfo.Symbol));
            await grain.SetTokenInfoAsync(tokenInfo);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetTokenInfoAsync Exception chainId {chainId} symbol {symbol}", tokenInfo.ChainId, tokenInfo.Symbol);
        }
    }

    public async Task<List<string>> GetBPAsync(string chainId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IBPGrain>(chainId);
            return await grain.GetBPAsync() ?? new List<string>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetBPAsync Exception chainId {chainId}", chainId);
            return new List<string>();
        }
    }

    public async Task<BpInfoDto> GetBPWithRoundAsync(string chainId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IBPGrain>(chainId);
            return await grain.GetBPWithRoundAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetBPWithRoundAsync Exception chainId {chainId}", chainId);
            return new BpInfoDto();
        }
    }

    public async Task SetBPAsync(string chainId, List<string> addressList, long round)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IBPGrain>(chainId);
            await grain.SetBPAsync(addressList, round);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetBPAsync Exception chainId {chainId}", chainId);
        }
    }

    public async Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType + chainId);
            return await grain.GetStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeight on chain {id} error", chainId);
            return CommonConstant.LongError;
        }
    }

    public async Task SetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType, long height)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() + chainId);
            await grain.SetStateAsync(height);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetIndexBlockHeight on chain {id} error", chainId);
        }
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
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

    public async Task<Dictionary<string, long>> GetHoldersAsync(List<string> symbols, string chainId, int skipCount, int maxResultCount)
    {
        try
        {
            var response = await _graphQlClientFactory.GetClient(GraphQLClientEnum.ModuleClient)
                .SendQueryAsync<IndexerTokenInfosDto>(new GraphQLRequest
                {
                    Query = @"query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$symbols:[String!]){
                        tokenInfo(input:{chainId: $chainId,skipCount: $skipCount,maxResultCount: $maxResultCount,symbols: $symbols})
                        {
                            totalCount,
                            items
                            {
                                symbol,
                                holderCount
                            } 
                        }}",
                    Variables = new
                    {
                        chainId, skipCount, maxResultCount, symbols
                    }
                });
            return response.Data?.TokenInfo?.Items?.ToDictionary(x => x.Symbol, x => x.HolderCount) ?? new Dictionary<string, long>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetHoldersAsyncException chainId={chainId}, symbol={symbol}", chainId, symbols);
        }
        return new Dictionary<string, long>();
    }
}