using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Common.GraphQL;
using GraphQL;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.DAO.Provider;

public interface IDAOProvider
{
    Task<List<IndexerDAOInfo>> GetSyncDAOListAsync(GetChainBlockHeightInput input);

    Task<DAOIndex> GetAsync(GetDAOInfoInput input);

    Task<Tuple<long, List<DAOIndex>>> GetDAOListAsync(QueryDAOListInput input, ISet<string> excludeNames);
    Task<Tuple<long, List<DAOIndex>>> GetDAOListByNameAsync(string chainId, List<string> names);
}

public class DAOProvider : IDAOProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<DAOIndex, string> _daoIndexRepository;

    public DAOProvider(IGraphQlHelper graphQlHelper,
        INESTRepository<DAOIndex, string> daoIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _daoIndexRepository = daoIndexRepository;
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
                        voterCount
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
}