using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using TomorrowDAOServer.Grains.Grain.Dao;
using TomorrowDAOServer.Grains.Grain.Election;
using TomorrowDAOServer.Grains.Grain.Proposal;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Providers;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Common.Provider;

public interface IGraphQLProvider
{
    public Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol);
    public Task SetTokenInfoAsync(TokenInfoDto tokenInfo);
    public Task<List<string>> GetBPAsync(string chainId);
    public Task<BpInfoDto> GetBPWithRoundAsync(string chainId);
    public Task SetBPAsync(string chainId, List<string> addressList, long round);
    public Task<long> GetProposalNumAsync(string chainId);
    public Task SetProposalNumAsync(string chainId, long parliamentCount, long associationCount, long referendumCount);
    public Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType);
    public Task SetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType, long height);
    public Task<long> GetIndexBlockHeightAsync(string chainId);

    public Task<Dictionary<string, long>> GetHoldersAsync(List<string> symbols, string chainId, int skipCount,
        int maxResultCount);

    public Task<List<DAOAmount>> GetDAOAmountAsync(string chainId);
    public Task SetHighCouncilMembersAsync(string chainId, string daoId, List<string> addressList);
    public Task<List<string>> GetHighCouncilMembersAsync(string chainId, string daoId);
    Task<int> SetDaoAliasInfoAsync(string chainId, string alias, DaoAliasDto daoAliasDto);
}

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly IGraphQLClient _graphQlClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly IGraphQlClientFactory _graphQlClientFactory;
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly IIndexerProvider _indexerProvider;

    public GraphQLProvider(IGraphQLClient graphQlClient, ILogger<GraphQLProvider> logger,
        IClusterClient clusterClient, IGraphQlClientFactory graphQlClientFactory, IGraphQlHelper graphQlHelper,
        IIndexerProvider indexerProvider)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _graphQlClientFactory = graphQlClientFactory;
        _graphQlClient = graphQlClient;
        _graphQlHelper = graphQlHelper;
        _indexerProvider = indexerProvider;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleGetTokenInfoAsync), ReturnDefault = ReturnDefault.New)]
    public async Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol)
    {
        var grain = _clusterClient.GetGrain<ITokenGrain>(GuidHelper.GenerateGrainId(chainId, symbol));
        return await grain.GetTokenInfoAsync();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleSetTokenInfoAsync))]
    public async Task SetTokenInfoAsync(TokenInfoDto tokenInfo)
    {
        var grain = _clusterClient.GetGrain<ITokenGrain>(GuidHelper.GenerateGrainId(tokenInfo.ChainId,
            tokenInfo.Symbol));
        await grain.SetTokenInfoAsync(tokenInfo);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetBPAsync Error", ReturnDefault = ReturnDefault.New)]
    public async Task<List<string>> GetBPAsync(string chainId)
    {
        var grain = _clusterClient.GetGrain<IBPGrain>(chainId);
        return await grain.GetBPAsync() ?? new List<string>();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetBPWithRoundAsync Error", ReturnDefault = ReturnDefault.New)]
    public async Task<BpInfoDto> GetBPWithRoundAsync(string chainId)
    {
        var grain = _clusterClient.GetGrain<IBPGrain>(chainId);
        return await grain.GetBPWithRoundAsync();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleSetBPAsync))]
    public async Task SetBPAsync(string chainId, List<string> addressList, long round)
    {
        var grain = _clusterClient.GetGrain<IBPGrain>(chainId);
        await grain.SetBPAsync(addressList, round);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetProposalNumAsync Error", ReturnDefault = ReturnDefault.Default)]
    public async Task<long> GetProposalNumAsync(string chainId)
    {
        var grain = _clusterClient.GetGrain<IProposalNumGrain>(chainId);
        return await grain.GetProposalNumAsync();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn))]
    public async Task SetProposalNumAsync(string chainId, long parliamentCount, long associationCount,
        long referendumCount)
    {
        var grain = _clusterClient.GetGrain<IProposalNumGrain>(chainId);
        await grain.SetProposalNumAsync(parliamentCount, associationCount, referendumCount);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleGetLastEndHeightAsync))]
    public async Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType)
    {
        var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType + chainId);
        return await grain.GetStateAsync();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleSetLastEndHeightAsync))]
    public async Task SetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType, long height)
    {
        var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() + chainId);
        await grain.SetStateAsync(height);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), ReturnDefault = ReturnDefault.Default)]
    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        return await _indexerProvider.GetSyncStateAsync(chainId);
    }
    
    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleGetHoldersAsync))]
    public async Task<Dictionary<string, long>> GetHoldersAsync(List<string> symbols, string chainId, int skipCount,
        int maxResultCount)
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
        return response.Data?.TokenInfo?.Items?.ToDictionary(x => x.Symbol, x => x.HolderCount) ??
               new Dictionary<string, long>();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), ReturnDefault = ReturnDefault.New)]
    public async Task<List<DAOAmount>> GetDAOAmountAsync(string chainId)
    {
        var response = await _graphQlHelper.QueryAsync<IndexerCommonResult<List<DAOAmount>>>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!) {
                        data:getDAOAmountRecord(input: {chainId:$chainId})
                        {
                            governanceToken,amount
                        }
                    }",
            Variables = new
            {
                chainId
            }
        });
        return response.Data ?? new List<DAOAmount>();
    }

    
    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleSetHighCouncilMembersAsync))]
    public async Task SetHighCouncilMembersAsync(string chainId, string daoId, List<string> addressList)
    {
        var grainId = GuidHelper.GenerateId(chainId, daoId);
        var grain = _clusterClient.GetGrain<IHighCouncilMembersGrain>(grainId);
        await grain.SaveHighCouncilMembersAsync(addressList);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), ReturnDefault = ReturnDefault.New)]
    public async Task<List<string>> GetHighCouncilMembersAsync(string chainId, string daoId)
    {
        var grainId = GuidHelper.GenerateId(chainId, daoId);
        var grain = _clusterClient.GetGrain<IHighCouncilMembersGrain>(grainId);
        return await grain.GetHighCouncilMembersAsync();
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReThrow), Message = "Set dao alias info error.")]
    public async Task<int> SetDaoAliasInfoAsync(string chainId, string alias, DaoAliasDto daoAliasDto)
    {
        Log.Information("Set dao alias info, input={0}", JsonConvert.SerializeObject(daoAliasDto));
        var grainId = GuidHelper.GenerateId(chainId, alias);
        var grain = _clusterClient.GetGrain<IDaoAliasGrain>(grainId);
        var result = await grain.SaveDaoAliasInfoAsync(daoAliasDto);
        Log.Information("Set dao alias info result: {0}", JsonConvert.SerializeObject(result));
        if (result.Success)
        {
            return result.Data;
        }

        throw new SystemException($"Set dao alias info error, msg={result.Message}");
    }
}