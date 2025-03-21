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
using TomorrowDAOServer.ResourceToken.Indexer;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.ResourceToken.Provider;

public interface IResourceTokenProvider
{
    Task BulkAddOrUpdateAsync(List<ResourceTokenIndex> list);
    Task<List<IndexerResourceTokenDto>> GetSyncResourceTokenDataAsync(int skipCount, string chainId, long startBlockHeight,
        long endBlockHeight, int maxResultCount);
    Task<List<ResourceTokenIndex>> GetByIdsAsync(List<string> ids);
    Task<List<ResourceTokenIndex>> GetNeedParseAsync(int skipCount);
    Task<List<ResourceTokenIndex>> GetLatestAsync(int limit, string type, string method = null);
    Task<Tuple<long, List<ResourceTokenIndex>>> GetPageListAsync(int skipCount, int maxResultCount, string order, string address);
    Task<List<ResourceTokenIndex>> GetAllByPeriodAsync(DateTime startTime, DateTime endTime, string type);
}

public class ResourceTokenProvider : IResourceTokenProvider, ISingletonDependency
{
    private readonly INESTRepository<ResourceTokenIndex, string> _resourceTokenRepository;
    private readonly ILogger<ResourceTokenProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;

    public ResourceTokenProvider(INESTRepository<ResourceTokenIndex, string> resourceTokenRepository, 
        ILogger<ResourceTokenProvider> logger, IGraphQlHelper graphQlHelper)
    {
        _resourceTokenRepository = resourceTokenRepository;
        _logger = logger;
        _graphQlHelper = graphQlHelper;
    }

    public async Task BulkAddOrUpdateAsync(List<ResourceTokenIndex> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return;
        }
        await _resourceTokenRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<IndexerResourceTokenDto>> GetSyncResourceTokenDataAsync(int skipCount, string chainId, long startBlockHeight, long endBlockHeight,
        int maxResultCount)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerResourceToken>(new GraphQLRequest
        {
            Query =
                @"query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!){
            dataList:getResourceTokenList(input: {skipCount:$skipCount,chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount})
            {
                id,transactionId,method,symbol,resourceAmount,baseAmount,chainId,feeAmount,blockHeight,transactionStatus,operateTime,
                transactionInfo { chainId,transactionId,from,to,methodName,portKeyContract,cAHash,realTo,realMethodName }
            }}",
            Variables = new
            {
                skipCount,
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount
            }
        });
        return graphQlResponse?.DataList ?? new List<IndexerResourceTokenDto>();
    }

    public async Task<List<ResourceTokenIndex>> GetByIdsAsync(List<string> ids)
    {
        if (ids.IsNullOrEmpty())
        {
            return new List<ResourceTokenIndex>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<ResourceTokenIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.Id).Terms(ids))
        };

        QueryContainer Filter(QueryContainerDescriptor<ResourceTokenIndex> f) => f.Bool(b => b.Must(mustQuery));

        return (await _resourceTokenRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<List<ResourceTokenIndex>> GetNeedParseAsync(int skipCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ResourceTokenIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Address).Value(CommonConstant.NeedParse))
        };
        QueryContainer Filter(QueryContainerDescriptor<ResourceTokenIndex> f) => f.Bool(b => b.Must(mustQuery));

        var tuple = await _resourceTokenRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending,
            sortExp: o => o.BlockHeight);
        return tuple.Item2;
    }

    public async Task<List<ResourceTokenIndex>> GetLatestAsync(int limit, string type, string method)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ResourceTokenIndex>, QueryContainer>>();
        if (!string.IsNullOrEmpty(method))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Method).Value(method)));
        }
        if (!string.IsNullOrEmpty(type))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(type)));
        }
        QueryContainer Filter(QueryContainerDescriptor<ResourceTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
        var tuple = await _resourceTokenRepository.GetListAsync(Filter, limit: limit, sortType: SortOrder.Descending,
            sortExp: o => o.BlockHeight);
        return tuple.Item2;
    }

    public async Task<Tuple<long, List<ResourceTokenIndex>>> GetPageListAsync(int skipCount, int maxResultCount, string order, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ResourceTokenIndex>, QueryContainer>>();
        if (!string.IsNullOrEmpty(address))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<ResourceTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _resourceTokenRepository.GetListAsync(Filter, skip: skipCount, limit: maxResultCount, 
            sortType: string.Equals(order, CommonConstant.Desc, StringComparison.OrdinalIgnoreCase) ? SortOrder.Descending : SortOrder.Ascending,
            sortExp: o => o.BlockHeight
        );
    }

    public async Task<List<ResourceTokenIndex>> GetAllByPeriodAsync(DateTime startTime, DateTime endTime, string type)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ResourceTokenIndex>, QueryContainer>>
        {
            q => q.DateRange(
                r => r.Field(f => f.OperateTime).GreaterThanOrEquals(startTime).LessThan(endTime)),
            q => q.Term(i => i.Field(f => f.Symbol).Value(type))
        };
        QueryContainer Filter(QueryContainerDescriptor<ResourceTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _resourceTokenRepository);
    }
}