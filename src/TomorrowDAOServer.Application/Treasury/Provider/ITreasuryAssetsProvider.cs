using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Treasury.Provider;

public interface ITreasuryAssetsProvider
{
    Task<GetTreasuryFundListResult> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input);
    Task<GetTreasuryFundListResult> GetAllTreasuryAssetsAsync(GetAllTreasuryAssetsInput input);
    Task<GetTreasuryRecordListResult> GetTreasuryRecordListAsync(GetTreasuryRecordListInput input);
}

public class TreasuryAssetsProvider : ITreasuryAssetsProvider, ISingletonDependency
{
    private readonly ILogger<TreasuryAssetsProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;

    public TreasuryAssetsProvider(ILogger<TreasuryAssetsProvider> logger, IGraphQlHelper graphQlHelper)
    {
        _logger = logger;
        _graphQlHelper = graphQlHelper;
    }

    public async Task<GetTreasuryFundListResult> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input)
    {
        var response = await _graphQlHelper.QueryAsync<IndexerCommonResult<GetTreasuryFundListResult>>(
            new GraphQLRequest
            {
                Query = @"
			    query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$daoId:String!,$symbols: [String],$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    data:getTreasuryFundList(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,daoId:$daoId,symbols:$symbols,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
                    {
                        item1,
                        item2 {
                            id,
                            chainId,
                            blockHeight,
                            daoId,
                            treasuryAddress,
                            symbol,
                            availableFunds,
                            lockedFunds
                        }
                    }
                }",
                Variables = new
                {
                    chainId = input.ChainId,
                    daoId = input.DaoId,
                    skipCount = input.SkipCount,
                    maxResultCount = input.MaxResultCount,
                    startBlockHeight = 0,
                    endBlockHeight = 0
                }
            });
        return response.Data ?? new GetTreasuryFundListResult();
    }

    public async Task<GetTreasuryFundListResult> GetAllTreasuryAssetsAsync(GetAllTreasuryAssetsInput input)
    {
        var response = await _graphQlHelper.QueryAsync<IndexerCommonResult<GetTreasuryFundListResult>>(
            new GraphQLRequest
            {
                Query = @"
			    query($chainId:String!,$daoId:String!) {
                    data:getAllTreasuryFundList(input: {chainId:$chainId,daoId:$daoId})
                    {
                        item1,
                        item2 {
                            id,
                            chainId,
                            blockHeight,
                            daoId,
                            treasuryAddress,
                            symbol,
                            availableFunds,
                            lockedFunds
                        }
                    }
                }",
                Variables = new
                {
                    chainId = input.ChainId,
                    daoId = input.DaoId,
                }
            });
        return response.Data ?? new GetTreasuryFundListResult();
    }

    public async Task<GetTreasuryRecordListResult> GetTreasuryRecordListAsync(GetTreasuryRecordListInput input)
    {
        var response = await _graphQlHelper.QueryAsync<IndexerCommonResult<GetTreasuryRecordListResult>>(
            new GraphQLRequest
            {
                Query = @"
			    query($chainId:String,$daoId:String,$treasuryAddress:String,$fromAddress:String,$skipCount:Int!,$maxResultCount:Int!,$symbols:[String!],$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    data:getTreasuryRecordList(input: {chainId:$chainId,daoId:$daoId,treasuryAddress:$treasuryAddress,fromAddress:$fromAddress,skipCount:$skipCount,maxResultCount:$maxResultCount,symbols:$symbols,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
                    {
                        item1,
                        item2 {
                            id,
                            chainId,
                            blockHeight,
                            daoId,
                            treasuryAddress,
                            amount,
                            symbol,
                            executor,
                            fromAddress,
                            toAddress,
                            memo,
                            treasuryRecordType,
                            createTime,
                            proposalId
                        }
                    }
                }",
                Variables = new
                {
                    chainId = input.ChainId,
                    daoId = input.DaoId,
                    treasuryAddress = input.TreasuryAddress,
                    fromAddress = input.Address,
                    symbols = input.Symbols,
                    skipCount = input.SkipCount,
                    maxResultCount = input.MaxResultCount,
                    startBlockHeight = input.StartBlockHeight,
                    endBlockHeight = input.EndBlockHeight
                }
            });
        return response.Data ?? new GetTreasuryRecordListResult();
    }
}