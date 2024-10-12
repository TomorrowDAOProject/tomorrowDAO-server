using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Users.Indexer;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.User.Provider;

public interface IUserBalanceProvider
{
    Task<List<UserBalance>> GetSyncUserBalanceListAsync(GetChainBlockHeightInput input);
    Task BulkAddOrUpdateAsync(List<UserBalanceIndex> list);
    Task<UserBalanceIndex> GetByIdAsync(string id);
    Task<List<UserBalanceIndex>> GetAllUserBalanceAsync(string chainId, string symbol, List<string> addressList);
}

public class UserBalanceProvider : IUserBalanceProvider, ISingletonDependency
{
    private readonly ILogger<UserBalanceProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<UserBalanceIndex, string> _userBalanceRepository;

    public UserBalanceProvider(ILogger<UserBalanceProvider> logger, IGraphQlHelper graphQlHelper, 
        INESTRepository<UserBalanceIndex, string> userBalanceRepository)
    {
        _logger = logger;
        _graphQlHelper = graphQlHelper;
        _userBalanceRepository = userBalanceRepository;
    }

    public async Task<List<UserBalance>> GetSyncUserBalanceListAsync(GetChainBlockHeightInput input)
    {
        try
        {
            var response = await _graphQlHelper.QueryAsync<IndexerUserBalance>(new GraphQLRequest
            {
                Query = @"
			    query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    getSyncUserBalanceInfos(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                        id,
                        chainId,
                        address,
                        amount,
                        symbol,
                        changeTime,
                        blockHeight,
                        
                    }
                }",
                Variables = new
                {
                    chainId = input.ChainId,
                    skipCount = input.SkipCount,
                    maxResultCount = input.MaxResultCount,
                    startBlockHeight = input.StartBlockHeight,
                    endBlockHeight = input.EndBlockHeight
                }
            });
            return response?.GetSyncUserBalanceInfos ?? new List<UserBalance>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSyncUserBalanceListAsyncException chainId {chainId} skip {skip} max {max} start {start} end {end}",
                input.ChainId, input.SkipCount, input.MaxResultCount, input.SkipCount, input.EndBlockHeight);
        }

        return new List<UserBalance>();
    }

    public async Task BulkAddOrUpdateAsync(List<UserBalanceIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }

        await _userBalanceRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<UserBalanceIndex> GetByIdAsync(string id)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.Id).Value(id))
        };
        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _userBalanceRepository.GetAsync(Filter);
    }

    public async Task<List<UserBalanceIndex>> GetAllUserBalanceAsync(string chainId, string symbol, List<string> addressList)
    {
        if (addressList.IsNullOrEmpty())
        {
            return new List<UserBalanceIndex>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.Symbol).Value(symbol)),
            q => q.Terms(i => i.Field(t => t.Address).Terms(addressList))
        };
        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _userBalanceRepository);
    }
    
    
}