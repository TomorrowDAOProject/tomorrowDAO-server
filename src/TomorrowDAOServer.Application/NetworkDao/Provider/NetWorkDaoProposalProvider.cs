using System;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.NetworkDao.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao.Provider;

public interface INetWorkDaoProposalProvider
{
    Task<NetworkDaoPagedResultDto<NetworkDaoProposalDto>>
        GetNetworkDaoProposalsAsync(GetNetworkDaoProposalsInput input);
}

public class NetWorkDaoProposalProvider : INetWorkDaoProposalProvider, ISingletonDependency
{
    private readonly ILogger<NetWorkDaoProposalProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;

    public NetWorkDaoProposalProvider(ILogger<NetWorkDaoProposalProvider> logger, IGraphQlHelper graphQlHelper)
    {
        _logger = logger;
        _graphQlHelper = graphQlHelper;
    }

    public async Task<NetworkDaoPagedResultDto<NetworkDaoProposalDto>> GetNetworkDaoProposalsAsync(
        GetNetworkDaoProposalsInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper
                .QueryAsync<IndexerCommonResult<NetworkDaoPagedResultDto<NetworkDaoProposalDto>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String!,$proposalIds:[String]!,$proposalType:NetworkDaoProposalType!){
            data:getNetworkDaoProposals(input: {skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,proposalIds:$proposalIds,proposalType:$proposalType})
            {            
                items {
                    proposalId,organizationAddress,title,description,proposalType,chainId,blockHash,blockHeight,previousBlockHash,isDeleted
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
                            proposalIds = input.ProposalIds,
                            proposalType = input.ProposalType
                        }
                    });
            return graphQlResponse?.Data ?? new NetworkDaoPagedResultDto<NetworkDaoProposalDto>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNetworkDaoProposalsAsync error, Param={Param}",
                JsonConvert.SerializeObject(input));
            throw;
        }
    }
}