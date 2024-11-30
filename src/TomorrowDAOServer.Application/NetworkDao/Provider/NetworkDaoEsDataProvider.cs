using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Nest;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao.Provider;

public interface INetworkDaoEsDataProvider
{
    Task BulkAddOrUpdateProposalIndexAsync(List<NetworkDaoProposalIndex> proposalList);
    Task BulkAddOrUpdateProposalListIndexAsync(List<NetworkDaoProposalListIndex> proposalListList);
    Task BulkAddOrUpdateProposalVoteIndexAsync(List<NetworkDaoProposalVoteIndex> voteIndices);
    Task BulkAddOrUpdateOrgIndexAsync(List<NetworkDaoOrgIndex> orgIndices);
    Task BulkAddOrUpdateOrgMemberIndexAsync(List<NetworkDaoOrgMemberIndex> orgMemberIndices);
    Task BulkAddOrUpdateOrgProposerIndexAsync(List<NetworkDaoOrgProposerIndex> orgProposerIndices);
    Task BulkDeleteOrgMemberIndexAsync(List<NetworkDaoOrgMemberIndex> orgMemberList);
    Task BulkDeleteOrgProposerIndexAsync(List<NetworkDaoOrgProposerIndex> orgProposerList);
    Task BulkAddOrUpdateVoteTeamAsync(List<NetworkDaoVoteTeamIndex> networkDaoVoteTeamIndices);
    Task<Tuple<long, List<NetworkDaoProposalListIndex>>> GetProposalListListAsync(GetProposalListInput request);
    Task<Tuple<long, List<NetworkDaoProposalIndex>>> GetProposalListAsync(GetProposalListInput getProposalListInput);
    Task<NetworkDaoProposalIndex> GetProposalIndexAsync(GetProposalInfoInput input);
    Task<Tuple<long, List<NetworkDaoOrgIndex>>> GetOrgIndexAsync(GetOrgListInput input);
    Task<Tuple<long, List<NetworkDaoOrgMemberIndex>>> GetOrgMemberListAsync(GetOrgMemberListInput input);

    Task<List<NetworkDaoOrgMemberIndex>>
        GetOrgMemberListByOrgAddressAsync(string chainId, List<string> orgAddressList);

    Task<Tuple<long, List<NetworkDaoOrgProposerIndex>>> GetOrgProposerListAsync(GetOrgProposerListInput input);

    Task<Tuple<long, List<NetworkDaoOrgProposerIndex>>> GetOrgProposerListByOrgAddressAsync(
        GetOrgProposerByOrgAddressInput input);


    Task<Tuple<long, List<NetworkDaoProposalVoteIndex>>> GetProposalVotedListAsync(GetVotedListInput input);
    Task<Tuple<long, List<NetworkDaoVoteTeamIndex>>> GetVoteTeamListAsync(GetVoteTeamListInput input);
}

public class NetworkDaoEsDataProvider : INetworkDaoEsDataProvider, ISingletonDependency
{
    private readonly ILogger<NetworkDaoEsDataProvider> _logger;
    private readonly INESTRepository<NetworkDaoProposalIndex, string> _proposalIndexRepository;
    private readonly INESTRepository<NetworkDaoProposalListIndex, string> _proposalListIndexRepository;
    private readonly INESTRepository<NetworkDaoProposalVoteIndex, string> _proposalVoteIndexRepository;
    private readonly INESTRepository<NetworkDaoOrgIndex, string> _orgIndexRepository;
    private readonly INESTRepository<NetworkDaoOrgMemberIndex, string> _orgMemberIndexRepository;
    private readonly INESTRepository<NetworkDaoOrgProposerIndex, string> _orgProposerIndexRepository;
    private readonly INESTRepository<NetworkDaoVoteTeamIndex, string> _voteTeamIndexRepository;

    public NetworkDaoEsDataProvider(ILogger<NetworkDaoEsDataProvider> logger,
        INESTRepository<NetworkDaoProposalIndex, string> proposalIndexRepository,
        INESTRepository<NetworkDaoProposalListIndex, string> proposalListIndexRepository,
        INESTRepository<NetworkDaoProposalVoteIndex, string> proposalVoteIndexRepository,
        INESTRepository<NetworkDaoOrgIndex, string> orgIndexRepository,
        INESTRepository<NetworkDaoOrgMemberIndex, string> orgMemberIndexRepository,
        INESTRepository<NetworkDaoOrgProposerIndex, string> orgProposerIndexRepository,
        INESTRepository<NetworkDaoVoteTeamIndex, string> voteTeamIndexRepository)
    {
        _logger = logger;
        _proposalIndexRepository = proposalIndexRepository;
        _proposalListIndexRepository = proposalListIndexRepository;
        _proposalVoteIndexRepository = proposalVoteIndexRepository;
        _orgIndexRepository = orgIndexRepository;
        _orgMemberIndexRepository = orgMemberIndexRepository;
        _orgProposerIndexRepository = orgProposerIndexRepository;
        _voteTeamIndexRepository = voteTeamIndexRepository;
    }

    public async Task BulkAddOrUpdateProposalIndexAsync(List<NetworkDaoProposalIndex> proposalList)
    {
        if (proposalList.IsNullOrEmpty())
        {
            return;
        }

        await _proposalIndexRepository.BulkAddOrUpdateAsync(proposalList);
    }

    public async Task BulkAddOrUpdateProposalListIndexAsync(List<NetworkDaoProposalListIndex> proposalListList)
    {
        if (proposalListList.IsNullOrEmpty())
        {
            return;
        }

        await _proposalListIndexRepository.BulkAddOrUpdateAsync(proposalListList);
    }

    public async Task BulkAddOrUpdateProposalVoteIndexAsync(List<NetworkDaoProposalVoteIndex> voteIndices)
    {
        if (voteIndices.IsNullOrEmpty())
        {
            return;
        }

        await _proposalVoteIndexRepository.BulkAddOrUpdateAsync(voteIndices);
    }

    public async Task BulkAddOrUpdateOrgIndexAsync(List<NetworkDaoOrgIndex> orgIndices)
    {
        if (orgIndices.IsNullOrEmpty())
        {
            return;
        }

        await _orgIndexRepository.BulkAddOrUpdateAsync(orgIndices);
    }

    public async Task BulkAddOrUpdateOrgMemberIndexAsync(List<NetworkDaoOrgMemberIndex> orgMemberIndices)
    {
        if (orgMemberIndices.IsNullOrEmpty())
        {
            return;
        }

        await _orgMemberIndexRepository.BulkAddOrUpdateAsync(orgMemberIndices);
    }

    public async Task BulkAddOrUpdateOrgProposerIndexAsync(List<NetworkDaoOrgProposerIndex> orgProposerIndices)
    {
        if (orgProposerIndices.IsNullOrEmpty())
        {
            return;
        }

        await _orgProposerIndexRepository.BulkAddOrUpdateAsync(orgProposerIndices);
    }
    
    public async Task BulkDeleteOrgMemberIndexAsync(List<NetworkDaoOrgMemberIndex> orgMemberList)
    {
        if (orgMemberList.IsNullOrEmpty())
        {
            return;
        }

        await _orgMemberIndexRepository.BulkDeleteAsync(orgMemberList);
    }

    public async Task BulkDeleteOrgProposerIndexAsync(List<NetworkDaoOrgProposerIndex> orgProposerList)
    {
        if (orgProposerList.IsNullOrEmpty())
        {
            return;
        }

        await _orgProposerIndexRepository.BulkDeleteAsync(orgProposerList);
    }

    public async Task BulkAddOrUpdateVoteTeamAsync(List<NetworkDaoVoteTeamIndex> networkDaoVoteTeamIndices)
    {
        if (networkDaoVoteTeamIndices.IsNullOrEmpty())
        {
            return;
        }
        await _voteTeamIndexRepository.BulkAddOrUpdateAsync(networkDaoVoteTeamIndices);
    }

    public async Task<Tuple<long, List<NetworkDaoProposalListIndex>>> GetProposalListListAsync(
        GetProposalListInput request)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoProposalListIndex>, QueryContainer>>();
        var contentShouldQuery =
            new List<Func<QueryContainerDescriptor<NetworkDaoProposalListIndex>, QueryContainer>>();
        AssemblyBaseQuery(request, mustQuery);
        AssemblyContentQuery(request, contentShouldQuery);

        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoProposalListIndex> f) =>
            f.Bool(b => contentShouldQuery.Any()
                ? b.Must(mustQuery)
                    .Should(s => s.Bool(sb => sb.Should(contentShouldQuery).MinimumShouldMatch(1)))
                : b.Must(mustQuery)
            );

        Func<SortDescriptor<NetworkDaoProposalListIndex>, IPromise<IList<ISort>>> sortDescriptor =
            _ => new SortDescriptor<NetworkDaoProposalListIndex>().Descending(a => a.CreatedAt);

        return await _proposalListIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor,
            skip: request.SkipCount,
            limit: request.MaxResultCount);
    }

    public async Task<Tuple<long, List<NetworkDaoProposalIndex>>> GetProposalListAsync(GetProposalListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoProposalIndex>, QueryContainer>>();
        var contentShouldQuery = new List<Func<QueryContainerDescriptor<NetworkDaoProposalIndex>, QueryContainer>>();
        AssemblyBaseQuery(input, mustQuery);
        AssemblyContentQuery(input, contentShouldQuery);

        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoProposalIndex> f) =>
            f.Bool(b => contentShouldQuery.Any()
                ? b.Must(mustQuery).Should(contentShouldQuery).MinimumShouldMatch(1)
                : b.Must(mustQuery)
            );

        // Func<SortDescriptor<NetworkDaoProposalIndex>, IPromise<IList<ISort>>> sortDescriptor =
        //     _ => new SortDescriptor<NetworkDaoProposalIndex>().Descending(a => a.BlockTime);

        // return await _proposalIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor,
        //     skip: input.SkipCount,
        //     limit: input.MaxResultCount);
        return await _proposalIndexRepository.GetListAsync(Filter,
            skip: input.SkipCount,
            limit: input.MaxResultCount);
    }

    public async Task<NetworkDaoProposalIndex> GetProposalIndexAsync(GetProposalInfoInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace() || input.ProposalId.IsNullOrWhiteSpace())
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoProposalIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)),
            q => q.Term(i => i.Field(t => t.ProposalId).Value(input.ProposalId))
        };

        if (!input.Address.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.Proposer).Value(input.Address)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoProposalIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _proposalIndexRepository.GetAsync(Filter);
    }

    public async Task<Tuple<long, List<NetworkDaoOrgIndex>>> GetOrgIndexAsync(GetOrgListInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
        {
            return new Tuple<long, List<NetworkDaoOrgIndex>>(0, new List<NetworkDaoOrgIndex>());
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoOrgIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId))
        };
        if (input.OrgType != NetworkDaoOrgType.All)
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.OrgType).Value(input.OrgType)));
        }

        if (!input.OrgAddress.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.OrgAddress).Value(input.OrgAddress)));
        }

        if (!input.OrgAddresses.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(t => t.OrgAddress).Terms(input.OrgAddresses)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoOrgIndex> f) => f.Bool(b => b.Must(mustQuery));

        Func<SortDescriptor<NetworkDaoOrgIndex>, IPromise<IList<ISort>>> sortDescriptor = null;
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            sortDescriptor =
                _ => new SortDescriptor<NetworkDaoOrgIndex>().Descending(a => a.CreatedAt);
        }
        else
        {
            sortDescriptor = CreateSortDescriptor<NetworkDaoOrgIndex>(input.Sorting);
        }

        return await _orgIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor,
            skip: input.SkipCount, limit: input.MaxResultCount);
    }

    public async Task<Tuple<long, List<NetworkDaoOrgMemberIndex>>> GetOrgMemberListAsync(GetOrgMemberListInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
        {
            return new Tuple<long, List<NetworkDaoOrgMemberIndex>>(0, new List<NetworkDaoOrgMemberIndex>());
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoOrgMemberIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId))
        };

        if (!input.OrgAddress.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.OrgAddress).Value(input.OrgAddress)));
        }
        
        if (!input.OrgAddresses.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(t => t.OrgAddress).Terms(input.OrgAddresses)));
        }

        if (!input.MemberAddress.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.Member).Value(input.MemberAddress)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoOrgMemberIndex> f) => f.Bool(b => b.Must(mustQuery));

        Func<SortDescriptor<NetworkDaoOrgMemberIndex>, IPromise<IList<ISort>>> sortDescriptor = null;
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            sortDescriptor =
                _ => new SortDescriptor<NetworkDaoOrgMemberIndex>().Descending(a => a.CreatedAt);
        }
        else
        {
            sortDescriptor = CreateSortDescriptor<NetworkDaoOrgMemberIndex>(input.Sorting);
        }

        return await _orgMemberIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor, skip: input.SkipCount,
            limit: input.MaxResultCount);
    }

    public async Task<List<NetworkDaoOrgMemberIndex>> GetOrgMemberListByOrgAddressAsync(string chainId,
        [ItemCanBeNull] List<string> orgAddressList)
    {
        if (chainId.IsNullOrWhiteSpace() || orgAddressList.IsNullOrEmpty())
        {
            return new List<NetworkDaoOrgMemberIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoOrgMemberIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Terms(i => i.Field(t => t.OrgAddress).Terms(orgAddressList))
        };

        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoOrgMemberIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _orgMemberIndexRepository.GetListAsync(Filter)).Item2 ?? new List<NetworkDaoOrgMemberIndex>();
    }

    public async Task<Tuple<long, List<NetworkDaoOrgProposerIndex>>> GetOrgProposerListAsync(
        GetOrgProposerListInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
        {
            return new Tuple<long, List<NetworkDaoOrgProposerIndex>>(0, new List<NetworkDaoOrgProposerIndex>());
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoOrgProposerIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)),
        };
        if (input.OrgType != NetworkDaoOrgType.All)
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.OrgType).Value(input.OrgType)));
        }
        if (!input.Address.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.Proposer).Value(input.Address)));
        }
        if (!input.OrgAddress.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.OrgAddress).Value(input.OrgAddress)));
        }
        if (!input.OrgAddresses.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(t => t.OrgAddress).Terms(input.OrgAddresses)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoOrgProposerIndex> f) => f.Bool(b => b.Must(mustQuery));

        Func<SortDescriptor<NetworkDaoOrgProposerIndex>, IPromise<IList<ISort>>> sortDescriptor = null;
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            sortDescriptor =
                _ => new SortDescriptor<NetworkDaoOrgProposerIndex>().Descending(a => a.Proposer);
        }
        else
        {
            sortDescriptor = CreateSortDescriptor<NetworkDaoOrgProposerIndex>(input.Sorting);
        }

        return await _orgProposerIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor, skip: input.SkipCount,
            limit: input.MaxResultCount);
    }

    public async Task<Tuple<long, List<NetworkDaoOrgProposerIndex>>> GetOrgProposerListByOrgAddressAsync(
        GetOrgProposerByOrgAddressInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace() || input.OrgAddressList.IsNullOrEmpty())
        {
            return new Tuple<long, List<NetworkDaoOrgProposerIndex>>(0, new List<NetworkDaoOrgProposerIndex>());
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoOrgProposerIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)),
            q => q.Terms(i => i.Field(t => t.OrgAddress).Terms(input.OrgAddressList))
        };

        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoOrgProposerIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _orgProposerIndexRepository.GetListAsync(Filter) ??
               new Tuple<long, List<NetworkDaoOrgProposerIndex>>(0, new List<NetworkDaoOrgProposerIndex>());
    }

    public async Task<Tuple<long, List<NetworkDaoProposalVoteIndex>>> GetProposalVotedListAsync(GetVotedListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoProposalVoteIndex>, QueryContainer>>()
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId))
        };

        if (!input.ProposalId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.ProposalId).Value(input.ProposalId)));
        }

        if (!input.ProposalIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(t => t.ProposalId).Terms(input.ProposalIds)));
        }

        if (input.ProposalType != NetworkDaoOrgType.All)
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.OrgType).Value(input.ProposalType)));
        }

        if (!input.Address.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.Address).Value(input.Address)));
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<NetworkDaoProposalVoteIndex>, QueryContainer>>();
        if (!input.Search.IsNullOrWhiteSpace())
        {
            shouldQuery.Add(q => q.Term(i => i.Field(t => t.Address).Value(input.Search)));
            shouldQuery.Add(q => q.Term(i => i.Field(t => t.TransactionInfo.TransactionId).Value(input.Search)));
        }

        //
        // QueryContainer Filter(QueryContainerDescriptor<NetworkDaoProposalVoteIndex> f) =>
        //     f.Bool(b => shouldQuery.Any()
        //         ? b.Must(mustQuery).Should(s => s.Bool(sb => sb.Should(shouldQuery).MinimumShouldMatch(1)))
        //         : b.Must(mustQuery)
        //     );
        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoProposalVoteIndex> f) =>
            f.Bool(b => shouldQuery.Any()
                ? b.Must(mustQuery).Should(shouldQuery).MinimumShouldMatch(1)
                : b.Must(mustQuery)
            );

        Func<SortDescriptor<NetworkDaoProposalVoteIndex>, IPromise<IList<ISort>>> sortDescriptor = null;
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            sortDescriptor =
                _ => new SortDescriptor<NetworkDaoProposalVoteIndex>().Descending(a => a.Time);
        }
        else
        {
            sortDescriptor = CreateSortDescriptor<NetworkDaoProposalVoteIndex>(input.Sorting);
        }

        return await _proposalVoteIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor,
            skip: input.SkipCount, limit: input.MaxResultCount);
    }

    public async Task<Tuple<long, List<NetworkDaoVoteTeamIndex>>> GetVoteTeamListAsync(GetVoteTeamListInput input)
    {
        if (input.ChainId.IsNullOrWhiteSpace())
        {
            return new Tuple<long, List<NetworkDaoVoteTeamIndex>>(0, new List<NetworkDaoVoteTeamIndex>());
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NetworkDaoVoteTeamIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId))
        };

        if (!input.PublicKey.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.PublicKey).Value(input.PublicKey)));
        }

        if (input.IsActive != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(t => t.IsActive).Value(input.IsActive)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NetworkDaoVoteTeamIndex> f) => f.Bool(b => b.Must(mustQuery));

        Func<SortDescriptor<NetworkDaoVoteTeamIndex>, IPromise<IList<ISort>>> sortDescriptor = null;
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            sortDescriptor =
                _ => new SortDescriptor<NetworkDaoVoteTeamIndex>().Descending(a => a.UpdateTime);
        }
        else
        {
            sortDescriptor = CreateSortDescriptor<NetworkDaoVoteTeamIndex>(input.Sorting);
        }

        return await _voteTeamIndexRepository.GetSortListAsync(Filter, sortFunc: sortDescriptor, skip: input.SkipCount,
            limit: input.MaxResultCount);
    }

    private static void AssemblyBaseQuery(GetProposalListInput input,
        List<Func<QueryContainerDescriptor<NetworkDaoProposalListIndex>, QueryContainer>> mustQuery)
    {
        if (!input.ChainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        if (!input.Address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.OrganizationAddress).Value(input.Address)));
        }

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.IsContractDeployed).Value(input.IsContract)));

        if (input.Status != NetworkDaoProposalStatusEnum.All)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Status).Value(input.Status)));
        }

        if (input.ProposalType != NetworkDaoOrgType.All)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.OrgType).Value(input.ProposalType)));
        }

        if (!input.ProposalIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.ProposalId).Terms(input.ProposalIds)));
        }
    }

    private static void AssemblyContentQuery(GetProposalListInput request,
        List<Func<QueryContainerDescriptor<NetworkDaoProposalListIndex>, QueryContainer>> shouldQuery)
    {
        if (string.IsNullOrWhiteSpace(request.Search))
        {
            return;
        }

        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalId).Query(request.Search)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ContractAddress).Query(request.Search)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.Proposer).Query(request.Search)));
    }

    private static void AssemblyBaseQuery(GetProposalListInput input,
        List<Func<QueryContainerDescriptor<NetworkDaoProposalIndex>, QueryContainer>> mustQuery)
    {
        if (!input.ChainId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        if (!input.Address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.OrganizationAddress).Value(input.Address)));
        }

        if (input.IsContract != null)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.IsContractDeployed).Value(input.IsContract)));
        }

        if (input.Status != NetworkDaoProposalStatusEnum.All)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Status).Value(input.Status)));
        }

        if (input.ProposalType != NetworkDaoOrgType.All)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.OrgType).Value(input.ProposalType)));
        }

        if (!input.ProposalIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.ProposalId).Terms(input.ProposalIds)));
        }

        if (!input.ProposalId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ProposalId).Value(input.ProposalId)));
        }

        if (input.Proposer.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Proposer).Value(input.Proposer)));
        }
    }

    private static void AssemblyContentQuery(GetProposalListInput request,
        List<Func<QueryContainerDescriptor<NetworkDaoProposalIndex>, QueryContainer>> shouldQuery)
    {
        if (string.IsNullOrWhiteSpace(request.Search))
        {
            return;
        }

        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ProposalId).Query(request.Search)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.ContractAddress).Query(request.Search)));
        shouldQuery.Add(q => q.Match(m => m.Field(f => f.Proposer).Query(request.Search)));
    }

    private static Func<SortDescriptor<T>, IPromise<IList<ISort>>> CreateSortDescriptor<T>(string sortString)
        where T : class
    {
        var sorting = sortString.Trim().Split(' ');
        var fieldName = sorting[0];
        var ascending = true;
        if (sorting.Length >= 2)
        {
            ascending = !sorting[1].Equals("DESC", StringComparison.OrdinalIgnoreCase);
        }

        return sortDesc => ascending
            ? sortDesc.Ascending(CreateSortExpression<T>(fieldName))
            : sortDesc.Descending(CreateSortExpression<T>(fieldName));
    }

    private static Expression<Func<T, object>> CreateSortExpression<T>(string fieldName)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.Convert(Expression.PropertyOrField(param, fieldName), typeof(object));
        return Expression.Lambda<Func<T, object>>(body, param);
    }
}