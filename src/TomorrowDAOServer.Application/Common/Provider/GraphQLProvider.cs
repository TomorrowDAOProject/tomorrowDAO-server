using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
// using AElf.ExceptionHandler;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common.GraphQL;
// using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using TomorrowDAOServer.Grains.Grain.Dao;
using TomorrowDAOServer.Grains.Grain.Election;
using TomorrowDAOServer.Grains.Grain.Proposal;
using TomorrowDAOServer.Grains.Grain.Sequence;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Grains.Grain.Users;
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleGetTokenInfoAsync), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleSetTokenInfoAsync))]
    public virtual async Task SetTokenInfoAsync(TokenInfoDto tokenInfo)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetBPAsync Error", ReturnDefault = ReturnDefault.New)]
    public virtual async Task<List<string>> GetBPAsync(string chainId)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetBPWithRoundAsync Error", ReturnDefault = ReturnDefault.New)]
    public virtual async Task<BpInfoDto> GetBPWithRoundAsync(string chainId)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleSetBPAsync))]
    public virtual async Task SetBPAsync(string chainId, List<string> addressList, long round)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), Message = "GetProposalNumAsync Error", ReturnDefault = ReturnDefault.Default)]
    public virtual async Task<long> GetProposalNumAsync(string chainId)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn))]
    public virtual async Task SetProposalNumAsync(string chainId, long parliamentCount, long associationCount,
        long referendumCount)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleGetLastEndHeightAsync))]
    public virtual async Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType)
    {
            // TestCode 
            //  {
            //      var userGrain = _clusterClient.GetGrain<IUserGrain>(Guid.Parse("cdba8eca-dde5-4b1f-8ea4-2e70f8f12d7e"));
            //      var grainResultDto = await userGrain.GetUser();
            //      _logger.LogInformation("grainResultDto={0}", JsonConvert.SerializeObject(grainResultDto));
            //
            //      var proposalNumGrain = _clusterClient.GetGrain<IProposalNumGrain>("tDVW");
            //      var proposalNumAsync = await proposalNumGrain.GetProposalNumAsync();
            //      _logger.LogInformation("proposalNumAsync={0}", JsonConvert.SerializeObject(proposalNumAsync));
            //
            //      var sequenceGrain = _clusterClient.GetGrain<ISequenceGrain>("TelegramAppSequence");
            //      var nextValAsync = await sequenceGrain.GetNextValAsync();
            //      _logger.LogInformation("nextValAsync={0}", JsonConvert.SerializeObject(nextValAsync));
            //
            //      var tokenExchangeGrain = _clusterClient.GetGrain<ITokenExchangeGrain>("ONEONEONE_USD");
            //      var tokenExchangeGrainDto = await tokenExchangeGrain.GetAsync();
            //      _logger.LogInformation("tokenExchangeGrainDto={0}", JsonConvert.SerializeObject(tokenExchangeGrainDto));
            //
            //      var tokenGrain = _clusterClient.GetGrain<ITokenGrain>("tDVW-ELF");
            //      var tokenInfoAsync = await tokenGrain.GetTokenInfoAsync();
            //      _logger.LogInformation("tokenInfoAsync={0}", JsonConvert.SerializeObject(tokenInfoAsync));
            //
            //      var highCouncilMembersGrain = _clusterClient.GetGrain<IHighCouncilMembersGrain>("tDVW_c83c2c3bb8487e278a20becf49ce5b6f6cc4d31f6175ac6c1ae1fdc21b18d76a");
            //      var highCouncilMembersAsync = await highCouncilMembersGrain.GetHighCouncilMembersAsync();
            //      _logger.LogInformation("highCouncilMembersAsync={0}", JsonConvert.SerializeObject(highCouncilMembersAsync));
            //
            //      var daoAliasGrain = _clusterClient.GetGrain<IDaoAliasGrain>("tDVW_18-17");
            //      var daoAliasInfoAsync = await daoAliasGrain.GetDaoAliasInfoAsync();
            //      _logger.LogInformation("daoAliasInfoAsync={0}", JsonConvert.SerializeObject(daoAliasInfoAsync));
            //
            //      var graphQlGrain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(WorkerBusinessType.BPInfoUpdate.ToString() + "tDVW");
            //      //await graphQlGrain.SetStateAsync(100);
            //      var stateAsync = await graphQlGrain.GetStateAsync();
            //      _logger.LogInformation("stateAsync={0}", JsonConvert.SerializeObject(stateAsync));
            //      
            //      var bpGrain = _clusterClient.GetGrain<IBPGrain>("tDVW");
            //      //await bpGrain.SetBPAsync(new List<string>() {"address1", "address2", "address3"}, 1);
            //      var bpList = await bpGrain.GetBPAsync();
            //      _logger.LogInformation("bpList={0}", JsonConvert.SerializeObject(bpList));
            //  }
        
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleSetLastEndHeightAsync))]
    public virtual async Task SetLastEndHeightAsync(string chainId, WorkerBusinessType queryChainType, long height)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), ReturnDefault = ReturnDefault.Default)]
    public virtual async Task<long> GetIndexBlockHeightAsync(string chainId)
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
    
    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleGetHoldersAsync))]
    public virtual async Task<Dictionary<string, long>> GetHoldersAsync(List<string> symbols, string chainId, int skipCount,
        int maxResultCount)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<List<DAOAmount>> GetDAOAmountAsync(string chainId)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleSetHighCouncilMembersAsync))]
    public virtual async Task SetHighCouncilMembersAsync(string chainId, string daoId, List<string> addressList)
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

    // [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
    //     MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), ReturnDefault = ReturnDefault.New)]
    public virtual async Task<List<string>> GetHighCouncilMembersAsync(string chainId, string daoId)
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