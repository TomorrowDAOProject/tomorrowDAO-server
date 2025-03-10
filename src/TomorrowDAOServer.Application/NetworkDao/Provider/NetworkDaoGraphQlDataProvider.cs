using System;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.NetworkDao.Index;
using TomorrowDAOServer.NetworkDao.Migrator.GraphQL;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao.Provider;

public interface INetworkDaoGraphQlDataProvider
{
    Task<PageResultDto<IndexerProposal>> GetNetworkDaoProposalIndexAsync(GetProposalIndexInput input);

    Task<PageResultDto<IndexerProposalReleased>>
        GetNetworkDaoProposalReleasedIndexAsync(GetProposalReleasedIndexInput input);

    Task<PageResultDto<IndexerProposalVoteRecord>> GetNetworkDaoProposalVoteRecordAsync(GetProposalVoteRecordIndexInput input);

    Task<PageResultDto<IndexerOrgChanged>> GetNetworkDaoOrgChangedIndexAsync(GetOrgChangedIndexInput input);
}

public class NetworkDaoGraphQlDataProvider : INetworkDaoGraphQlDataProvider, ISingletonDependency
{
    private readonly ILogger<NetworkDaoGraphQlDataProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;

    public NetworkDaoGraphQlDataProvider(ILogger<NetworkDaoGraphQlDataProvider> logger, IGraphQlHelper graphQlHelper)
    {
        _logger = logger;
        _graphQlHelper = graphQlHelper;
    }

    public async Task<PageResultDto<IndexerProposal>> GetNetworkDaoProposalIndexAsync(GetProposalIndexInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper
                .QueryAsync<IndexerCommonResult<PageResultDto<IndexerProposal>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String,$orgAddresses:[String!],$orgType:NetworkDaoOrgType!,$proposalIds:[String!],$title:String,$contractNames:[String!],$methodNames:[String!]){
            data:getNetworkDaoProposalIndex(input: {skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,orgAddresses:$orgAddresses,orgType:$orgType,proposalIds:$proposalIds,title:$title,contractNames:$contractNames,methodNames:$methodNames})
            {
                data {
                    id,proposalId,organizationAddress,title,description,orgType,isReleased,saveTime,symbol,totalAmount,chainId,blockHash,blockHeight,blockTime,previousBlockHash,isDeleted,
                    transactionInfo {
                        chainId,transactionId,from,to,methodName,isAAForwardCall,portKeyContract,cAHash,realTo,realMethodName
                    }
                },
                totalCount
            }}",
                        Variables = new
                        {
                            skipCount = input.SkipCount,
                            maxResultCount = input.MaxResultCount,
                            startBlockHeight = input.StartBlockHeight,
                            endBlockHeight = input.EndBlockHeight,
                            chainId = input.ChainId,
                            orgAddresses = input.OrgAddresses,
                            orgType = input.OrgType,
                            proposalIds = input.ProposalIds,
                            title = input.Title,
                            contractNames = input.ContractNames,
                            methodNames = input.MethodNames
                        }
                    });
            return graphQlResponse?.Data ?? new PageResultDto<IndexerProposal>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNetworkDaoProposalsAsync error, Param={Param}", JsonConvert.SerializeObject(input));
            throw;
        }
    }

    public async Task<PageResultDto<IndexerProposalReleased>> GetNetworkDaoProposalReleasedIndexAsync(GetProposalReleasedIndexInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper
                .QueryAsync<IndexerCommonResult<PageResultDto<IndexerProposalReleased>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String,$orgAddresses:[String!],$orgType:NetworkDaoOrgType!,$proposalIds:[String!],$title:String){
            data:getNetworkDaoProposalReleasedIndex(input: {skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,orgAddresses:$orgAddresses,orgType:$orgType,proposalIds:$proposalIds,title:$title})
            {
                data {
                    id,proposalId,organizationAddress,title,description,orgType,chainId,blockHash,blockHeight,blockTime,previousBlockHash,isDeleted,
                    transactionInfo {
                        chainId,transactionId,from,to,methodName,isAAForwardCall,portKeyContract,cAHash,realTo,realMethodName
                    }
                },
                totalCount
            }}",
                        Variables = new
                        {
                            skipCount = input.SkipCount,
                            maxResultCount = input.MaxResultCount,
                            startBlockHeight = input.StartBlockHeight,
                            endBlockHeight = input.EndBlockHeight,
                            chainId = input.ChainId,
                            orgAddresses = input.OrgAddresses,
                            orgType = input.OrgType,
                            proposalIds = input.ProposalIds,
                            title = input.Title
                        }
                    });
            return graphQlResponse?.Data ?? new PageResultDto<IndexerProposalReleased>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNetworkDaoProposalReleasedAsync error, Param={Param}", JsonConvert.SerializeObject(input));
            throw;
        }
    }

    public async Task<PageResultDto<IndexerProposalVoteRecord>> GetNetworkDaoProposalVoteRecordAsync(GetProposalVoteRecordIndexInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper
                .QueryAsync<IndexerCommonResult<PageResultDto<IndexerProposalVoteRecord>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String,$orgAddresses:[String!],$orgType:NetworkDaoOrgType!,$proposalIds:[String!],$receiptType:ReceiptTypeEnum){
            data:getNetworkDaoProposalVoteRecordIndex(input: {skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,orgAddresses:$orgAddresses,orgType:$orgType,proposalIds:$proposalIds,receiptType:$receiptType})
            {
                data {
                    id,proposalId,address,receiptType,time,organizationAddress,orgType,symbol,amount, chainId,blockHash,blockHeight,blockTime,previousBlockHash,isDeleted,
                    transactionInfo {
                        chainId,transactionId,from,to,methodName,isAAForwardCall,portKeyContract,cAHash,realTo,realMethodName
                    }
                },
                totalCount
            }}",
                        Variables = new
                        {
                            skipCount = input.SkipCount,
                            maxResultCount = input.MaxResultCount,
                            startBlockHeight = input.StartBlockHeight,
                            endBlockHeight = input.EndBlockHeight,
                            chainId = input.ChainId,
                            orgAddresses = input.OrgAddresses,
                            orgType = input.OrgType,
                            proposalIds = input.ProposalIds,
                            receiptType = input.ReceiptType
                        }
                    });
            return graphQlResponse?.Data ?? new PageResultDto<IndexerProposalVoteRecord>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNetworkDaoProposalVoteRecordAsync error, Param={Param}", JsonConvert.SerializeObject(input));
            throw;
        }
    }
    
    public async Task<PageResultDto<IndexerOrgChanged>> GetNetworkDaoOrgChangedIndexAsync(GetOrgChangedIndexInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper
                .QueryAsync<IndexerCommonResult<PageResultDto<IndexerOrgChanged>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String,$orgAddresses:[String!],$orgType:NetworkDaoOrgType!){
            data:getNetworkDaoOrgChangedIndex(input: {skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,orgAddresses:$orgAddresses,orgType:$orgType})
            {
                data {
                    id,organizationAddress,orgType, chainId,blockHash,blockHeight,blockTime,previousBlockHash,isDeleted,
                    transactionInfo {
                        chainId,transactionId,from,to,methodName,isAAForwardCall,portKeyContract,cAHash,realTo,realMethodName
                    }
                },
                totalCount
            }}",
                        Variables = new
                        {
                            skipCount = input.SkipCount,
                            maxResultCount = input.MaxResultCount,
                            startBlockHeight = input.StartBlockHeight,
                            endBlockHeight = input.EndBlockHeight,
                            chainId = input.ChainId,
                            orgAddresses = input.OrgAddresses,
                            orgType = input.OrgType
                        }
                    });
            return graphQlResponse?.Data ?? new PageResultDto<IndexerOrgChanged>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNetworkDaoOrgChangedIndexAsync error, Param={Param}", JsonConvert.SerializeObject(input));
            throw;
        }
    }
}