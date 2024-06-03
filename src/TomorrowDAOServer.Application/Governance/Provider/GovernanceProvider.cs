using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Governance.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Governance.Provider;

public interface IGovernanceProvider
{
    Task<IndexerGovernanceSchemeDto> GetGovernanceSchemeAsync(string chainId, string daoId);
}

public class GovernanceProvider : IGovernanceProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;

    public GovernanceProvider(IGraphQlHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<IndexerGovernanceSchemeDto> GetGovernanceSchemeAsync(string chainId, string daoId)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerGovernanceSchemeDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String, $daoId:String){
            data:getGovernanceSchemeIndex(input: {chainId:$chainId,dAOId:$daoId})
            {
                id,
                dAOId,
                schemeId,
                schemeAddress,
                chainId,
                governanceMechanism,
                governanceToken,
                createTime,
                minimalRequiredThreshold,
                minimalVoteThreshold,
                minimalApproveThreshold,
                maximalRejectionThreshold,
                maximalAbstentionThreshold,
                proposalThreshold
            }}",
            Variables = new
            {
                chainId = chainId,
                daoId = daoId
            }
        });
        return graphQlResponse ?? new IndexerGovernanceSchemeDto();
    }
}