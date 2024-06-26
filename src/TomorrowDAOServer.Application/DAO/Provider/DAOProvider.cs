using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Common.GraphQL;
using GraphQL;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.DAO.Provider;

public interface IDAOProvider
{
    Task<List<IndexerDAOInfo>> GetSyncDAOListAsync(GetChainBlockHeightInput input);

    Task<DAOIndex> GetAsync(GetDAOInfoInput input);

    Task<Tuple<long, List<DAOIndex>>> GetDAOListAsync(QueryDAOListInput input, ISet<string> excludeNames);
    Task<long> GetDAOListCountAsync(QueryDAOListInput input, ISet<string> excludeNames);
    Task<Tuple<long, List<DAOIndex>>> GetDAOListByNameAsync(string chainId, List<string> names);
    Task<Tuple<long, List<DAOIndex>>> GetMyOwneredDAOListAsync(QueryMyDAOListInput input, string address);
    Task<DAOIndex> GetNetworkDAOAsync(string chainId);
    Task<PageResultDto<IndexerDAOInfo>> GetMyParticipatedDaoListAsync(GetParticipatedInput input);
    Task<PageResultDto<MemberDto>> GetMemberListAsync(GetMemberListInput listInput);
    Task<MemberDto> GetMemberAsync(GetMemberInput listInput);
}

public class DAOProvider : IDAOProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<DAOIndex, string> _daoIndexRepository;
    private readonly ILogger<DAOProvider> _logger;

    public DAOProvider(IGraphQlHelper graphQlHelper, ILogger<DAOProvider> logger,
        INESTRepository<DAOIndex, string> daoIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _daoIndexRepository = daoIndexRepository;
        _logger = logger;
    }

    public async Task<List<IndexerDAOInfo>> GetSyncDAOListAsync(GetChainBlockHeightInput input)
    {
        var response =  await _graphQlHelper.QueryAsync<IndexerDAOInfos>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$startBlockHeight:Long!,$endBlockHeight:Long!) {
                    dAOInfos:getDAOList(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                        id,
                        chainId,
                        blockHeight,
                        creator,
                        metadata {
                            name,
                            logoUrl,
                            description,
                            socialMedia
                        },
                        governanceToken,
                        isHighCouncilEnabled,
                        highCouncilAddress,
                        maxHighCouncilMemberCount,
                        maxHighCouncilCandidateCount,
                        electionPeriod,
                        stakingAmount,
                        highCouncilTermNumber,
                        fileInfoList,
                        isTreasuryContractNeeded,
                        subsistStatus,
                        treasuryContractAddress,
                        treasuryAccountAddress,
                        isTreasuryPause,
                        treasuryPauseExecutor,
                        voteContractAddress,
                        electionContractAddress,
                        governanceContractAddress,
                        timelockContractAddress,
                        activeTimePeriod,
                        vetoActiveTimePeriod,
                        pendingTimePeriod,
                        executeTimePeriod,
                        vetoExecuteTimePeriod,
                        createTime,
                        isNetworkDAO,
                        voterCount,
                        governanceMechanism
                    }
                }",
            Variables = new
            {
                chainId = input.ChainId,
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                startBlockHeight = input.StartBlockHeight,
                endBlockHeight = input.EndBlockHeight
            }
        });
        return response?.DAOInfos ?? new List<IndexerDAOInfo>();
    }
    
    public async Task<DAOIndex> GetAsync(GetDAOInfoInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)));
        mustQuery.Add(q => q.Term(i => i.Field(t => t.Id).Value(input.DAOId)));

        QueryContainer Filter(QueryContainerDescriptor<DAOIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _daoIndexRepository.GetAsync(Filter);
    }
    
    public async Task<Tuple<long, List<DAOIndex>>> GetDAOListAsync(QueryDAOListInput input, ISet<string> excludeNames)
    {
        var chainId = input.ChainId;
        var mustQuery = new List<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>
        {
            q => 
                q.Term(i => i.Field(t => t.ChainId).Value(chainId))
        };
        if (!excludeNames.IsNullOrEmpty())
        {
            mustQuery.Add(q => 
                !q.Terms(i => i.Field(t => t.Metadata.Name).Terms(excludeNames)));
        }
        QueryContainer Filter(QueryContainerDescriptor<DAOIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _daoIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount, 
            sortFunc: _ => new SortDescriptor<DAOIndex>().Descending(index => index.CreateTime));
    }

    public async Task<long> GetDAOListCountAsync(QueryDAOListInput input, ISet<string> excludeNames)
    {
        var chainId = input.ChainId;
        var mustQuery = new List<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>
        {
            q => 
                q.Term(i => i.Field(t => t.ChainId).Value(chainId))
        };
        if (!excludeNames.IsNullOrEmpty())
        {
            mustQuery.Add(q => 
                !q.Terms(i => i.Field(t => t.Metadata.Name).Terms(excludeNames)));
        }
        QueryContainer Filter(QueryContainerDescriptor<DAOIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _daoIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<Tuple<long, List<DAOIndex>>> GetDAOListByNameAsync(string chainId, List<string> names)
    {
        if (names.IsNullOrEmpty())
        {
            return new Tuple<long, List<DAOIndex>>(0, new List<DAOIndex>());
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>
        {
            q => 
                q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => 
                q.Terms(i => i.Field(t => t.Metadata.Name).Terms(names))
        };
        QueryContainer Filter(QueryContainerDescriptor<DAOIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _daoIndexRepository.GetSortListAsync(Filter);
    }
    
    public async Task<Tuple<long, List<DAOIndex>>> GetMyOwneredDAOListAsync(QueryMyDAOListInput input, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>
        {
            q => 
                q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)),
            q => 
                q.Term(i => i.Field(t => t.Creator).Value(address))
        };
        QueryContainer Filter(QueryContainerDescriptor<DAOIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _daoIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount, 
            sortFunc: _ => new SortDescriptor<DAOIndex>().Descending(index => index.CreateTime));
    }

    public async Task<DAOIndex> GetNetworkDAOAsync(string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<DAOIndex>, QueryContainer>>
        {
            q => 
                q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => 
                q.Term(i => i.Field(t => t.IsNetworkDAO).Value(true))
        };
        QueryContainer Filter(QueryContainerDescriptor<DAOIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _daoIndexRepository.GetAsync(Filter);
    }

    public async Task<PageResultDto<IndexerDAOInfo>> GetMyParticipatedDaoListAsync(GetParticipatedInput input)
    {
        try
        {
            var response =  await _graphQlHelper.QueryAsync<IndexerCommonResult<PageResultDto<IndexerDAOInfo>>>(new GraphQLRequest
            {
                Query = @"
			        query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$address:String!) {
                        data:getMyParticipated(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,address:$address})
                        {
                            totalCount,
                            data{
                                id,
                                chainId,
                                blockHeight,
                                creator,
                                metadata {
                                    name,
                                    logoUrl,
                                    description,
                                    socialMedia
                                },
                                governanceToken,
                                isHighCouncilEnabled,
                                highCouncilAddress,
                                maxHighCouncilMemberCount,
                                maxHighCouncilCandidateCount,
                                electionPeriod,
                                stakingAmount,
                                highCouncilTermNumber,
                                fileInfoList,
                                isTreasuryContractNeeded,
                                subsistStatus,
                                treasuryContractAddress,
                                treasuryAccountAddress,
                                isTreasuryPause,
                                treasuryPauseExecutor,
                                voteContractAddress,
                                electionContractAddress,
                                governanceContractAddress,
                                timelockContractAddress,
                                activeTimePeriod,
                                vetoActiveTimePeriod,
                                pendingTimePeriod,
                                executeTimePeriod,
                                vetoExecuteTimePeriod,
                                createTime,
                                isNetworkDAO,
                                voterCount
                            }
                        }
                    }",
                Variables = new
                {
                    chainId = input.ChainId,
                    skipCount = input.SkipCount,
                    maxResultCount = input.MaxResultCount,
                    address = input.Address
                }
            });
            return response?.Data ?? new PageResultDto<IndexerDAOInfo>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetMyParticipatedDaoListAsyncException chainId {chainId}, address {address}, skipCount {skipCount}, maxResultCount {maxResultCount}", 
                input.ChainId, input.Address, input.SkipCount, input.MaxResultCount);
            return new PageResultDto<IndexerDAOInfo>();
        }
        
    }

    public async Task<PageResultDto<MemberDto>> GetMemberListAsync(GetMemberListInput listInput)
    {
        try
        {
            var response =  await _graphQlHelper.QueryAsync<IndexerCommonResult<PageResultDto<MemberDto>>>(new GraphQLRequest
            {
                Query = @"
			        query($chainId:String!,$skipCount:Int!,$maxResultCount:Int!,$dAOId:String!) {
                        data:getMemberList(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,dAOId:$dAOId})
                        {
                            totalCount,
                            data{
                                id,
                                chainId,
                                blockHeight,
                                dAOId,    
                                address,
                                createTime
                            }
                        }
                    }",
                Variables = new
                {
                    chainId = listInput.ChainId,
                    skipCount = listInput.SkipCount,
                    maxResultCount = listInput.MaxResultCount,
                    dAOId = listInput.DAOId
                }
            });
            return response?.Data ?? new PageResultDto<MemberDto>();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetMemberListAsync chainId {chainId}, daoId {daoId}, skipCount {skipCount}, maxResultCount {maxResultCount}", 
                listInput.ChainId, listInput.DAOId, listInput.SkipCount, listInput.MaxResultCount);
            return new PageResultDto<MemberDto>();
        }
    }

    public async Task<MemberDto> GetMemberAsync(GetMemberInput input)
    {
        try
        {
            var response =  await _graphQlHelper.QueryAsync<IndexerCommonResult<MemberDto>>(new GraphQLRequest
            {
                Query = @"
			        query($chainId:String!,$dAOId:String!,$address:String!) {
                        data:getMember(input: {chainId:$chainId,dAOId:$dAOId,address:$address})
                        {
                            id,
                            chainId,
                            blockHeight,
                            dAOId,    
                            address,
                            createTime
                        }
                    }",
                Variables = new
                {
                    chainId = input.ChainId,
                    dAOId = input.DAOId,
                    address = input.Address
                }
            });
            return response?.Data ?? new MemberDto();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIsMemberAsync chainId {chainId}, daoId {daoId}, address {address}", 
                input.ChainId, input.DAOId, input.Address);
            return new MemberDto();
        }
    }
}