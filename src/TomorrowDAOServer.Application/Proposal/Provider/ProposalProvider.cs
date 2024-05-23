using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Proposal.Provider;

public interface IProposalProvider
{
    Task<List<IndexerProposal>> GetSyncProposalDataAsync(int skipCount, string chainId, long startBlockHeight,
        long endBlockHeight, int maxResultCount);

    public Task<Tuple<long, List<ProposalIndex>>> GetProposalListAsync(QueryProposalListInput input);

    public Task<ProposalIndex> GetProposalByIdAsync(string chainId, string proposalId);

    public Task<List<ProposalIndex>> GetProposalByIdsAsync(string chainId, List<string> proposalIds);

    public Task<long> GetProposalCountByDAOIds(string chainId, string DAOId);

    public Task BulkAddOrUpdateAsync(List<ProposalIndex> list);

    public Task<List<ProposalIndex>> GetNonFinishedProposalListAsync(int skipCount, List<ProposalStage> stageList);
    
    public Task<List<ProposalIndex>> GetNeedChangeProposalListAsync(int skipCount);

    public Task<Tuple<long, List<ProposalIndex>>> QueryProposalsByProposerAsync(QueryProposalByProposerRequest request);
}

public class ProposalProvider : IProposalProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<ProposalIndex, string> _proposalIndexRepository;

    public ProposalProvider(IGraphQlHelper graphQlHelper,
        INESTRepository<ProposalIndex, string> proposalIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _proposalIndexRepository = proposalIndexRepository;
    }

    public async Task<List<IndexerProposal>> GetSyncProposalDataAsync(int skipCount, string chainId,
        long startBlockHeight, long endBlockHeight, int maxResultCount)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerProposalSync>(new GraphQLRequest
        {
            Query =
                @"query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!){
            dataList:getSyncProposalInfos(input: {skipCount:$skipCount,chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount})
            {
                id,chainId,blockHeight,
                dAOId,proposalId,proposalTitle,proposalDescription,forumUrl,proposalType,
                activeStartTime,activeEndTime,executeStartTime,executeEndTime,
                proposalStatus,proposalStage,proposer,schemeAddress,
                transaction {
                    toAddress,contractMethodName,params
                },            
                voteSchemeId,vetoProposalId,beVetoProposalId,deployTime,executeTime,
                governanceMechanism,
                minimalRequiredThreshold,minimalVoteThreshold,minimalApproveThreshold,
                maximalRejectionThreshold,maximalAbstentionThreshold,
                activeTimePeriod,vetoActiveTimePeriod,pendingTimePeriod,executeTimePeriod,vetoExecuteTimePeriod,isNetworkDAO
            }}",
            Variables = new
            {
                skipCount,
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount
            }
        });
        return graphQlResponse?.DataList ?? new List<IndexerProposal>();
    }

    public async Task<Tuple<long, List<ProposalIndex>>> GetProposalListAsync(QueryProposalListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        var shouldQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        AssemblyBaseQuery(input, mustQuery);

        AssemblyContentQuery(input.Content, shouldQuery);

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => shouldQuery.Any()
                ? b.Should(shouldQuery).MinimumShouldMatch(1).Must(mustQuery)
                : b.Must(mustQuery));

        //add sorting
        var sortDescriptor = GetDescendingDeployTimeSortDescriptor();

        return await _proposalIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor,
            skip: input.SkipCount,
            limit: input.MaxResultCount);
    }

    public async Task<ProposalIndex> GetProposalByIdAsync(string chainId, string proposalId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ProposalId).Value(proposalId)));

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return await _proposalIndexRepository.GetAsync(Filter);
    }

    public async Task<List<ProposalIndex>> GetProposalByIdsAsync(string chainId, List<string> proposalIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.ProposalId).Terms(proposalIds)));

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return (await _proposalIndexRepository.GetListAsync(Filter)).Item2;
    }

    public async Task<Tuple<long, List<ProposalIndex>>> QueryProposalsByProposerAsync(
        QueryProposalByProposerRequest request)
    {
        if (request == null || request.Proposer.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("");
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Proposer).Value(request.Proposer)));

        if (!request.ChainId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(request.ChainId)));
        }

        if (!request.DaoId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.DAOId).Value(request.DaoId)));
        }

        if (request.ProposalStage != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalStage).Value(request.ProposalStage)));
        }

        // if (request.ProposalStatus != null)
        // {
        //     mustQuery.Add(q => q.Term(i =>
        //         i.Field(f => f.ProposalStatus).Value(request.ProposalStatus)));
        // }

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) => f.Bool(b => b.Must(mustQuery));

        var sortDescriptor = GetAscendingDeployTimeSortDescriptor();

        return await _proposalIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor,
            skip: request.SkipCount,
            limit: request.MaxResultCount);
    }

    public async Task<long> GetProposalCountByDAOIds(string chainId, string DAOId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.ChainId).Terms(chainId)));
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.DAOId).Terms(DAOId)));

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return (await _proposalIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task BulkAddOrUpdateAsync(List<ProposalIndex> list)
    {
        await _proposalIndexRepository.BulkAddOrUpdateAsync(list);
    }

    public async Task<List<ProposalIndex>> GetNonFinishedProposalListAsync(int skipCount, List<ProposalStage> stageList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => !q.Terms(i =>
                i.Field(f => f.ProposalStage).Terms(stageList))
        };

        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var tuple = await _proposalIndexRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending,
            sortExp: o => o.BlockHeight);
        return tuple.Item2;
    }
    
    public async Task<List<ProposalIndex>> GetNeedChangeProposalListAsync(int skipCount)
    {
        var currentStr = DateTime.UtcNow.ToString("O");
        var activeMustQuery = GetNeedChangeMustQuery(ProposalStage.Active,
            tr => tr.Field(f => f.ActiveEndTime.ToUtcMilliSeconds()).LessThan(currentStr));
        var pendingMustQuery = GetNeedChangeMustQuery(ProposalStage.Pending,
            tr => tr.Field(f => f.ExecuteStartTime.ToUtcMilliSeconds()).LessThan(currentStr));;
        var executeMustQuery = GetNeedChangeMustQuery(ProposalStage.Execute,
            tr => tr.Field(f => f.ExecuteEndTime.ToUtcMilliSeconds()).LessThan(currentStr));;
        var shouldQuery = new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Bool(b => b.Must(activeMustQuery)),
            q => q.Bool(b => b.Must(pendingMustQuery)),
            q => q.Bool(b => b.Must(executeMustQuery))
        };
        QueryContainer Filter(QueryContainerDescriptor<ProposalIndex> f) =>
            f.Bool(b => b.Should(shouldQuery).MinimumShouldMatch(1));
       
        var tuple = await _proposalIndexRepository.GetListAsync(Filter, skip: skipCount, sortType: SortOrder.Ascending,
            sortExp: o => o.BlockHeight);
        return tuple.Item2;
    }

    private List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> GetNeedChangeMustQuery(ProposalStage proposalStage, Func<TermRangeQueryDescriptor<ProposalIndex>, ITermRangeQuery> selector)
    {
        return new List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>
        {
            q => q.Term(t => t.Field(f => f.ProposalStage).Value(proposalStage)),
            q => q.TermRange(selector)
        };
    }

    private static void AssemblyBaseQuery(QueryProposalListInput input,
        List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> mustQuery)
    {
        if (!input.ChainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        if (!input.DaoId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.DAOId).Value(input.DaoId)));
        }

        if (input.GovernanceMechanism != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.GovernanceMechanism).Value(input.GovernanceMechanism)));
        }

        if (input.ProposalType != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalType).Value(input.ProposalType)));
        }

        if (input.ProposalStatus != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalStatus).Value(input.ProposalStatus)));
        }
    }

    private static void AssemblyContentQuery(string content,
        List<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>> shouldQuery)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalId).Query(content)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalTitle).Query(content)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalDescription).Query(content)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.Proposer).Query(content)));
    }

    private static Func<SortDescriptor<ProposalIndex>, IPromise<IList<ISort>>> GetDescendingDeployTimeSortDescriptor()
    {
        //use default
        var sortDescriptor = new SortDescriptor<ProposalIndex>();
        sortDescriptor.Descending(a => a.DeployTime);
        return _ => sortDescriptor;
    }

    private static Func<SortDescriptor<ProposalIndex>, IPromise<IList<ISort>>> GetAscendingDeployTimeSortDescriptor()
    {
        //use default
        var sortDescriptor = new SortDescriptor<ProposalIndex>();
        sortDescriptor.Ascending(a => a.DeployTime);
        return _ => sortDescriptor;
    }
}