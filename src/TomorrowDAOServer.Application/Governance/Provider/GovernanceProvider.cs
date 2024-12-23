using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using Serilog;
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
    private readonly ILogger<GovernanceProvider> _logger;

    public GovernanceProvider(IGraphQlHelper graphQlHelper, ILogger<GovernanceProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
    }

    public async Task<IndexerGovernanceSchemeDto> GetGovernanceSchemeAsync(string chainId, string daoId)
    {
        var sw = Stopwatch.StartNew();
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
        
        sw.Stop();
        Log.Information("GetDAOByIdDuration: GetGovernanceScheme {0}", sw.ElapsedMilliseconds);
        
        return graphQlResponse ?? new IndexerGovernanceSchemeDto();
    }
}