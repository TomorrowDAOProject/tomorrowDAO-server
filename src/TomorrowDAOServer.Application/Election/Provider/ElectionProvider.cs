using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Index;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Election.Provider;

public interface IElectionProvider
{
    Task<ElectionPageResultDto<ElectionCandidateElectedDto>> GetCandidateElectedRecordsAsync(
        GetCandidateElectedRecordsInput input);

    Task<ElectionPageResultDto<ElectionHighCouncilConfigDto>>
        GetHighCouncilConfigAsync(GetHighCouncilConfigInput input);

    Task<ElectionPageResultDto<ElectionVotingItemDto>> GetVotingItemAsync(GetVotingItemInput input);

    Task<List<string>> GetHighCouncilMembersAsync(string chainId, string daoId);

    Task<List<HighCouncilManagedDaoIndex>> GetHighCouncilManagedDaoIndexAsync(
        GetHighCouncilMemberManagedDaoInput input);

    Task SaveOrUpdateHighCouncilManagedDaoIndexAsync(List<HighCouncilManagedDaoIndex> data);
    Task DeleteHighCouncilManagedDaoIndexAsync(List<HighCouncilManagedDaoIndex> data);
}

public class ElectionProvider : IElectionProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ILogger<ElectionProvider> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INESTRepository<HighCouncilManagedDaoIndex, string> _highCouncilManagedDaoRepository;

    public ElectionProvider(IGraphQlHelper graphQlHelper, ILogger<ElectionProvider> logger,
        IGraphQLProvider graphQlProvider,
        INESTRepository<HighCouncilManagedDaoIndex, string> highCouncilManagedDaoRepository)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _highCouncilManagedDaoRepository = highCouncilManagedDaoRepository;
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

    public async Task<List<HighCouncilManagedDaoIndex>> GetHighCouncilManagedDaoIndexAsync(
        GetHighCouncilMemberManagedDaoInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<HighCouncilManagedDaoIndex>, QueryContainer>>();

        if (!input.ChainId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        if (!input.DaoId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.DaoId).Value(input.DaoId)));
        }

        if (!input.MemberAddress.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.MemberAddress).Value(input.MemberAddress)));
        }


        QueryContainer Filter(QueryContainerDescriptor<HighCouncilManagedDaoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result =
            await _highCouncilManagedDaoRepository.GetListAsync(Filter, skip: input.SkipCount,
                limit: input.MaxResultCount);
        return result?.Item2 ?? new List<HighCouncilManagedDaoIndex>();
    }

    public async Task SaveOrUpdateHighCouncilManagedDaoIndexAsync(List<HighCouncilManagedDaoIndex> data)
    {
        await _highCouncilManagedDaoRepository.BulkAddOrUpdateAsync(data);
    }

    public async Task DeleteHighCouncilManagedDaoIndexAsync(List<HighCouncilManagedDaoIndex> data)
    {
        await _highCouncilManagedDaoRepository.BulkDeleteAsync(data);
    }
}