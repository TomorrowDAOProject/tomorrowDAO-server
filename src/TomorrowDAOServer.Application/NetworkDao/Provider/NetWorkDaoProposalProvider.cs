using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao.Provider;

public interface INetworkDaoProposalProvider
{
    Task<NetworkDaoPagedResultDto<NetworkDaoProposalDto>>
        GetNetworkDaoProposalsAsync(GetNetworkDaoProposalsInput input);

    Task<NetworkDaoProposalStatusEnum> GetNetworkDaoProposalStatusAsync(string chainId,
        NetworkDaoProposalIndex proposalIndex, NetworkDaoOrgIndex orgIndex, List<string> bpList);
    Task<bool> IsProposalVoteEndedAsync(string chainId, NetworkDaoProposalStatusEnum status, DateTime expiredTime);
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
        LogTargets = new[] { "input" })]
    public virtual async Task<NetworkDaoPagedResultDto<NetworkDaoProposalDto>> GetNetworkDaoProposalsAsync(
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

    public async Task<NetworkDaoProposalStatusEnum> GetNetworkDaoProposalStatusAsync(string chainId,
        NetworkDaoProposalIndex proposalIndex,
        NetworkDaoOrgIndex orgIndex, List<string> bpList)
    {
        if (proposalIndex.Status == NetworkDaoProposalStatusEnum.Released)
        {
            return proposalIndex.Status;
        }
        
        var now = DateTime.UtcNow;
        if (proposalIndex.ExpiredTime < now)
        {
            return NetworkDaoProposalStatusEnum.Expired;
        }

        if (proposalIndex.OrgType == NetworkDaoOrgType.Parliament)
        {
            var bpCount = bpList.Count;
            var total = proposalIndex.Approvals + proposalIndex.Rejections + proposalIndex.Abstentions;
            var approvalsPercentage = (proposalIndex.Approvals / bpCount) * 10000;
            var rejectionsPercentage = (proposalIndex.Rejections / bpCount) * 10000;
            var abstentionsPercentage = (proposalIndex.Abstentions / bpCount) * 10000;
            var totalPercentage = (total / bpCount) * 10000;
            if (approvalsPercentage >= orgIndex.MinimalApprovalThreshold 
                && rejectionsPercentage < orgIndex.MaximalRejectionThreshold 
                && abstentionsPercentage < orgIndex.MaximalAbstentionThreshold 
                && totalPercentage >= orgIndex.MinimalVoteThreshold)
            {
                return NetworkDaoProposalStatusEnum.Approved;
            }
        } else if (proposalIndex.OrgType == NetworkDaoOrgType.Association)
        {
            var total = proposalIndex.Approvals + proposalIndex.Rejections + proposalIndex.Abstentions;
            if (proposalIndex.Approvals >= orgIndex.MinimalApprovalThreshold 
                && proposalIndex.Rejections < orgIndex.MaximalRejectionThreshold 
                && proposalIndex.Abstentions < orgIndex.MaximalAbstentionThreshold
                && total >= orgIndex.MinimalVoteThreshold)
            {
                return NetworkDaoProposalStatusEnum.Approved;
            }
        }
        else
        {
            var total = proposalIndex.Approvals + proposalIndex.Rejections + proposalIndex.Abstentions;
            if (proposalIndex.Approvals >= orgIndex.MinimalApprovalThreshold 
                && proposalIndex.Rejections < orgIndex.MaximalRejectionThreshold 
                && proposalIndex.Abstentions < orgIndex.MaximalAbstentionThreshold
                && total >= orgIndex.MinimalVoteThreshold)
            {
                return NetworkDaoProposalStatusEnum.Approved;
            }
        }

        return proposalIndex.Status;
    }

    public async Task<bool> IsProposalVoteEndedAsync(string chainId, NetworkDaoProposalStatusEnum status, DateTime expiredTime)
    {
        if (status == NetworkDaoProposalStatusEnum.Released)
        {
            return true;
        }
        
        var now = DateTime.UtcNow;
        if (expiredTime < now)
        {
            return true;
        }
        return false;
    }
}