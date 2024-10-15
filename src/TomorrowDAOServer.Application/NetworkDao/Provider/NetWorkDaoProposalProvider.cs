using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.NetworkDao.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao.Provider;

public interface INetworkDaoProposalProvider
{
    Task<NetworkDaoPagedResultDto<NetworkDaoProposalDto>>
        GetNetworkDaoProposalsAsync(GetNetworkDaoProposalsInput input);
}

public class NetworkDaoProposalProvider : INetworkDaoProposalProvider, ISingletonDependency
{
    private readonly ILogger<NetworkDaoProposalProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;

    public NetworkDaoProposalProvider(ILogger<NetworkDaoProposalProvider> logger, IGraphQlHelper graphQlHelper)
    {
        _logger = logger;
        _graphQlHelper = graphQlHelper;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReThrowMethodName, 
        Message = "GetNetworkDaoProposalsAsync error",
        LogTargets = new []{"input"})]
    public async Task<NetworkDaoPagedResultDto<NetworkDaoProposalDto>> GetNetworkDaoProposalsAsync(
        GetNetworkDaoProposalsInput input)
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
}