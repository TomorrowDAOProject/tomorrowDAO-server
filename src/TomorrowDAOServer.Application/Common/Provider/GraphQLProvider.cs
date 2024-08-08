using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using TomorrowDAOServer.Grains.Grain.Dao;
using TomorrowDAOServer.Grains.Grain.Election;
using TomorrowDAOServer.Grains.Grain.Proposal;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Providers;
using Volo.Abp;
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
    public Task<Dictionary<string, long>> GetHoldersAsync(List<string> symbols, string chainId, int skipCount, int maxResultCount);
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

    public async Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ITokenGrain>(GuidHelper.GenerateGrainId(chainId, symbol));
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
            var grain = _clusterClient.GetGrain<ITokenGrain>(GuidHelper.GenerateGrainId(tokenInfo.ChainId, tokenInfo.Symbol));
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
        Stopwatch sw = Stopwatch.StartNew();
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
        finally
        {
            sw.Stop();
            _logger.LogInformation("GetDAOByIdDuration: GetBPWithRound {0}", sw.ElapsedMilliseconds);
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

    public async Task<long> GetProposalNumAsync(string chainId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IProposalNumGrain>(chainId);
            return await grain.GetProposalNumAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetProposalNumAsyncException chainId {chainId}", chainId);
            return 0;
        }
    }

    public async Task SetProposalNumAsync(string chainId, long parliamentCount, long associationCount, long referendumCount)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IProposalNumGrain>(chainId);
            await grain.SetProposalNumAsync(parliamentCount, associationCount, referendumCount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetProposalNumAsyncException chainId {chainId}", chainId);
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
        try
        {
            return await _indexerProvider.GetSyncStateAsync(chainId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeightAsync Exception on chain {chainId}", chainId);
            return 0;
        }
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

    public async Task<List<DAOAmount>> GetDAOAmountAsync(string chainId)
    {
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, "GetDAOAmountAsyncException chainId={chainId}", chainId);
        }

        return new List<DAOAmount>();
    }

    public async Task SetHighCouncilMembersAsync(string chainId, string daoId, List<string> addressList)
    {
        try
        {
            var grainId = GuidHelper.GenerateId(chainId, daoId);
            var grain = _clusterClient.GetGrain<IHighCouncilMembersGrain>(grainId);
            await grain.SaveHighCouncilMembersAsync(addressList);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetHighCouncilMembersAsync error: chain={id},DaoId={daoId}", chainId, daoId);
        }
    }

    public async Task<List<string>> GetHighCouncilMembersAsync(string chainId, string daoId)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            var grainId = GuidHelper.GenerateId(chainId, daoId);
            var grain = _clusterClient.GetGrain<IHighCouncilMembersGrain>(grainId);
            return await grain.GetHighCouncilMembersAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetHighCouncilMembersAsync error: chain={id},DaoId={daoId}", chainId, daoId);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("GetDAOByIdDuration: GetHighCouncilMembers {0}", sw.ElapsedMilliseconds);
        }

        return new List<string>();
    }

    public async Task<int> SetDaoAliasInfoAsync(string chainId, string alias, DaoAliasDto daoAliasDto)
    {
        try
        {
            _logger.LogInformation("Set dao alias info, input={0}", JsonConvert.SerializeObject(daoAliasDto));
            var grainId = GuidHelper.GenerateId(chainId, alias);
            var grain = _clusterClient.GetGrain<IDaoAliasGrain>(grainId);
            var result = await grain.SaveDaoAliasInfoAsync(daoAliasDto);
            _logger.LogInformation("Set dao alias info result: {0}", JsonConvert.SerializeObject(result));
            if (result.Success)
            {
                return result.Data;
            }
            
            throw new UserFriendlyException("Set dao alias info error, msg={0}", result.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Set dao alias info error.");
            throw;
        }
    }
}