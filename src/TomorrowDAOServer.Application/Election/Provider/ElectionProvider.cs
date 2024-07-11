using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Index;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Election.Provider;

public interface IElectionProvider
{
    Task<PagedResultDto<IndexerElection>> GetHighCouncilListAsync(GetHighCouncilListInput input);

    Task<ElectionPageResultDto<ElectionCandidateElectedDto>> GetCandidateElectedRecordsAsync(
        GetCandidateElectedRecordsInput input);

    Task<ElectionPageResultDto<ElectionHighCouncilConfigDto>>
        GetHighCouncilConfigAsync(GetHighCouncilConfigInput input);

    Task<ElectionPageResultDto<ElectionVotingItemDto>> GetVotingItemAsync(GetVotingItemInput input);

    Task<List<string>> GetHighCouncilMembersAsync(string chainId, string daoId);
}

public class ElectionProvider : IElectionProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ILogger<ElectionProvider> _logger;
    private readonly IGraphQLProvider _graphQlProvider;

    public ElectionProvider(IGraphQlHelper graphQlHelper, ILogger<ElectionProvider> logger,
        IGraphQLProvider graphQlProvider)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
        _graphQlProvider = graphQlProvider;
    }

    [Obsolete]
    public async Task<PagedResultDto<IndexerElection>> GetHighCouncilListAsync(GetHighCouncilListInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerElectionResult>(new GraphQLRequest
            {
                Query =
                    @"query($sorting:String,$skipCount:Int!,$maxMaxResultCount:Int!,$chainId:String,$DAOId:String,$highCouncilType:String,$termNumber:Long!){
            data:getHighCouncilListAsync(input: {sorting:$sorting,skipCount:$skipCount,maxMaxResultCount:$maxMaxResultCount,chainId:$chainId,DAOId:$DAOId,highCouncilType:$highCouncilType,termNumber:$termNumber})
                dataList{
                    id,chainId,DAOId,termNumber,highCouncilType,address,votesAmount,stakeAmount
                }
                ,totalCount
            }",
                Variables = new
                {
                    input.Sorting,
                    input.SkipCount,
                    input.MaxResultCount,
                    input.ChainId,
                    input.DAOId,
                    input.HighCouncilType,
                    input.TermNumber
                }
            });
            return new PagedResultDto<IndexerElection>
            {
                TotalCount = graphQlResponse.TotalCount,
                Items = graphQlResponse.DataList
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetHighCouncilListAsyncError chainId {chainId} DAOId {DAOId} termNumber{termNumber}",
                input.ChainId, input.DAOId, input.TermNumber);
        }

        return new PagedResultDto<IndexerElection>(0, new List<IndexerElection>());
    }

    public async Task<ElectionPageResultDto<ElectionCandidateElectedDto>> GetCandidateElectedRecordsAsync(
        GetCandidateElectedRecordsInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper
                .QueryAsync<IndexerCommonResult<ElectionPageResultDto<ElectionCandidateElectedDto>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String!,$daoId:String!){
            data:getElectionCandidateElected(input:{skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,daoId:$daoId})
            {
                items {
                    id,daoId,preTermNumber,newNumber,candidateElectedTime,chainId,blockHash,blockHeight,previousBlockHash,isDeleted
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
                            daoId = input.DaoId ?? string.Empty
                        }
                    });
            return graphQlResponse?.Data ?? new ElectionPageResultDto<ElectionCandidateElectedDto>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetCandidateElectedRecordsAsync error, chainId={chainId}, DAOId={DAOId},Param={Param}",
                input.ChainId, input.DaoId, JsonConvert.SerializeObject(input));
            throw;
        }
    }

    public async Task<ElectionPageResultDto<ElectionHighCouncilConfigDto>> GetHighCouncilConfigAsync(
        GetHighCouncilConfigInput input)
    {
        try
        {
            var graphQlResponse = await _graphQlHelper
                .QueryAsync<IndexerCommonResult<ElectionPageResultDto<ElectionHighCouncilConfigDto>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String!,$daoId:String!){
            data:getElectionHighCouncilConfig(input: {skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,daoId:$daoId})
            {            
                items {
                    id,daoId,maxHighCouncilMemberCount,maxHighCouncilCandidateCount,electionPeriod,isRequireHighCouncilForExecution,governanceToken,stakeThreshold,initialHighCouncilMembers,chainId,blockHash,blockHeight,previousBlockHash,isDeleted
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
                            daoId = input.DaoId ?? string.Empty
                        }
                    });
            return graphQlResponse?.Data ?? new ElectionPageResultDto<ElectionHighCouncilConfigDto>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetHighCouncilConfigAsync error, chainId={chainId}, DAOId={DAOId},Param={Param}",
                input.ChainId, input.DaoId, JsonConvert.SerializeObject(input));
            throw;
        }
    }

    public async Task<ElectionPageResultDto<ElectionVotingItemDto>> GetVotingItemAsync(GetVotingItemInput input)
    {
        try
        {
            var graphQlResponse =
                await _graphQlHelper.QueryAsync<IndexerCommonResult<ElectionPageResultDto<ElectionVotingItemDto>>>(
                    new GraphQLRequest
                    {
                        Query =
                            @"query($skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String!,$daoId:String!){
            data:getElectionVotingItem(input: {skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId,daoId:$daoId})
            {    
                items {
                    id,daoId,votingItemId,acceptedCurrency,isLockToken,currentSnapshotNumber,totalSnapshotNumber,options,registerTimestamp,startTimestamp,endTimestamp,currentSnapshotStartTimestamp,sponsor,isQuadratic,ticketCost,chainId,blockHash,blockHeight,previousBlockHash,isDeleted
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
                            daoId = input.DaoId ?? string.Empty
                        }
                    });
            return graphQlResponse?.Data ?? new ElectionPageResultDto<ElectionVotingItemDto>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetVotingItemAsync error, chainId={chainId}, DAOId={DAOId},Param={Param}",
                input.ChainId, input.DaoId, JsonConvert.SerializeObject(input));
            throw;
        }
    }

    public async Task<List<string>> GetHighCouncilMembersAsync(string chainId, string daoId)
    {
        return await _graphQlProvider.GetHighCouncilMembersAsync(chainId, daoId);
    }
}