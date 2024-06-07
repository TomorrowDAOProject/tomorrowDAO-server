using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.DAO.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.User;
using TomorrowDAOServer.Vote.Provider;

namespace TomorrowDAOServer.DAO;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class DAOAppService : ApplicationService, IDAOAppService
{
    private readonly IDAOProvider _daoProvider;
    private readonly IElectionProvider _electionProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IVoteProvider _voteProvider;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IOptionsMonitor<DaoOption> _testDaoOptions;
    private readonly IGovernanceProvider _governanceProvider;
    private readonly IUserService _userService;
    private const int ZeroSkipCount = 0;
    private const int GetMemberListMaxResultCount = 100;
    private const int CandidateTermNumber = 0;
    private ValueTuple<long, long> ProposalCountCache = new(0, 0);

    public DAOAppService(IDAOProvider daoProvider, IElectionProvider electionProvider, IGovernanceProvider governanceProvider,
        IProposalProvider proposalProvider, IExplorerProvider explorerProvider, IGraphQLProvider graphQlProvider,
        IVoteProvider voteProvider, IOptionsMonitor<DaoOption> testDaoOptions)
    {
        _daoProvider = daoProvider;
        _electionProvider = electionProvider;
        _proposalProvider = proposalProvider;
        _graphQlProvider = graphQlProvider;
        _voteProvider = voteProvider;
        _testDaoOptions = testDaoOptions;
        _explorerProvider = explorerProvider;
        _governanceProvider = governanceProvider;
    }

    public async Task<DAOInfoDto> GetDAOByIdAsync(GetDAOInfoInput input)
    {
        var daoIndex = await _daoProvider.GetAsync(input);
        var daoInfo = ObjectMapper.Map<DAOIndex, DAOInfoDto>(daoIndex);
        var governanceScheme = (await _governanceProvider.GetGovernanceSchemeAsync(input.ChainId, input.DAOId)).Data;
        daoInfo.OfGovernanceSchemeThreshold(governanceScheme.FirstOrDefault());
        if (!daoInfo.IsNetworkDAO)
        {
            //todo hc info
            return daoInfo;
        }

        var bpInfo = await _graphQlProvider.GetBPWithRoundAsync(input.ChainId);
        daoInfo.HighCouncilTermNumber = bpInfo.Round;
        daoInfo.HighCouncilMemberCount = bpInfo.AddressList.Count;
        return daoInfo;
    }

    public async Task<PagedResultDto<HcMemberDto>> GetMemberListAsync(GetHcMemberInput input)
    {
        var type = input.Type.IsNullOrWhiteSpace() ? HighCouncilType.Member.ToString() : input.Type;
        var daoInfo = await _daoProvider.GetAsync(input);
        var result = await _electionProvider.GetHighCouncilListAsync(new GetHighCouncilListInput
        {
            ChainId = input.ChainId,
            DAOId = input.DAOId,
            HighCouncilType = type,
            TermNumber = type == HighCouncilType.Member.ToString()
                ? daoInfo?.HighCouncilTermNumber ?? 0
                : CandidateTermNumber,
            MaxResultCount = (int)(type == HighCouncilType.Member.ToString()
                ? daoInfo?.HighCouncilConfig?.MaxHighCouncilMemberCount ?? GetMemberListMaxResultCount
                : input.MaxResultCount),
            SkipCount = type == HighCouncilType.Member.ToString() ? ZeroSkipCount : input.SkipCount,
            Sorting = input.Sorting
        });
        return new PagedResultDto<HcMemberDto>
        {
            TotalCount = result?.TotalCount ?? 0,
            Items = result?.Items?.Select(x => new HcMemberDto
            {
                Type = type,
                Address = AddressHelper.ToFullAddress(x.ChainId, x.Address),
                VotesAmount = x.VotesAmount.ToString(),
                StakeAmount = x.StakeAmount.ToString()
            }).ToList() ?? new List<HcMemberDto>()
        };
    }

    public async Task<PagedResultDto<DAOListDto>> GetDAOListAsync(QueryDAOListInput input)
    {
        var daoOption = _testDaoOptions.CurrentValue;
        var begin = input.SkipCount;
        var end = begin + input.MaxResultCount;
        var topCount = daoOption.TopDaoNames.Count;
        var excludeNames = new HashSet<string>(daoOption.FilteredDaoNames.Union(daoOption.TopDaoNames));
        if (begin >= topCount)
        {
            input.SkipCount -= topCount;
            return new PagedResultDto<DAOListDto> { Items = await GetNormalSearchList(input, excludeNames) };
        }

        List<DAOListDto> searchByNameList;
        if (end <= topCount)
        {
            searchByNameList = await GetNameSearchList(input, daoOption.TopDaoNames.Skip(begin).Take(end - begin).ToList());
            return new PagedResultDto<DAOListDto> {Items = searchByNameList};
        }

        searchByNameList = await GetNameSearchList(input, daoOption.TopDaoNames.Skip(begin).Take(topCount - begin).ToList());
        input.SkipCount = 0;
        input.MaxResultCount = end - topCount;
        var normalSearchList = await GetNormalSearchList(input, excludeNames);
        var combineList = new List<DAOListDto>();
        combineList.AddRange(searchByNameList);
        combineList.AddRange(normalSearchList);
        return new PagedResultDto<DAOListDto> { Items = combineList };
    }

    private async Task<List<DAOListDto>> GetNormalSearchList(QueryDAOListInput input, ISet<string> excludeNames)
    {
        return await FillDaoListAsync(input.ChainId,
            await _daoProvider.GetDAOListAsync(input, excludeNames));
    }

    private async Task<List<DAOListDto>> GetNameSearchList(QueryDAOListInput input, List<string> searchNames)
    {
        return (await FillDaoListAsync(input.ChainId,
                await _daoProvider.GetDAOListByNameAsync(input.ChainId, searchNames)))
            .OrderBy(x => searchNames.IndexOf(x.Name)).ToList();
    }

    private async Task<List<DAOListDto>> FillDaoListAsync(string chainId, Tuple<long, List<DAOIndex>> originResult)
    {
        var daoList = originResult.Item2;
        var items = ObjectMapper.Map<List<DAOIndex>, List<DAOListDto>>(daoList);
        var symbols = items.Select(x => x.Symbol.ToUpper()).Distinct().ToList();
        var tokenInfos = new Dictionary<string, TokenInfoDto>();
        foreach (var symbol in symbols)
        {
            tokenInfos[symbol] = await _explorerProvider.GetTokenInfoAsync(chainId, symbol);
        }

        foreach (var dao in items)
        {
            if (!dao.Symbol.IsNullOrEmpty())
            {
                dao.SymbolHoldersNum = tokenInfos.TryGetValue(dao.Symbol.ToUpper(), out var tokenInfo)
                    ? long.Parse(tokenInfo.Holders)
                    : 0L;
            }

            dao.ProposalsNum = await _proposalProvider.GetProposalCountByDAOIds(chainId, dao.DaoId);
            if (!dao.IsNetworkDAO)
            {
                continue;
            }

            dao.HighCouncilMemberCount = (await _graphQlProvider.GetBPAsync(chainId)).Count;
            if (DateTime.UtcNow.ToUtcMilliSeconds() - ProposalCountCache.Item2 >= 10 * 60 * 1000)
            {
                var parliamentTask = GetCountTask(Common.Enum.ProposalType.Parliament);
                var associationTask = GetCountTask(Common.Enum.ProposalType.Association);
                var referendumTask = GetCountTask(Common.Enum.ProposalType.Referendum);
                await Task.WhenAll(parliamentTask, associationTask, referendumTask);
                dao.ProposalsNum += (await parliamentTask).Total + (await associationTask).Total +
                                    (await referendumTask).Total;
                ProposalCountCache = new ValueTuple<long, long>(dao.ProposalsNum, DateTime.UtcNow.ToUtcMilliSeconds());
            }
            else
            {
                dao.ProposalsNum += ProposalCountCache.Item1;
            }
        }

        return items;
    }

    public async Task<List<string>> GetBPList(string chainId)
    {
        return await _graphQlProvider.GetBPAsync(chainId);
    }

    public async Task<List<MyDAOListDto>> GetMyDAOListAsync(QueryMyDAOListInput input)
    {
        var address = input.Address;
        var result = new List<MyDAOListDto>();
        if (address.IsNullOrEmpty())
        {
            return result;
        }

        switch (input.Type)
        {
            case MyDAOType.All:
                var ownedTask = GetMyOwnedDaoListDto(input, address);  
                var participatedTask = GetMyParticipatedDaoListDto(input, address);
                var managedTask = GetMyManagedDaoListDto(input);
                await Task.WhenAll(ownedTask, participatedTask, managedTask);
                result.Add(await ownedTask);  
                result.Add(await participatedTask); 
                result.Add(await managedTask); 
                break;
            case MyDAOType.Owned:
                result.Add(await GetMyOwnedDaoListDto(input, address));
                break;
            case MyDAOType.Participated:
                result.Add(await GetMyParticipatedDaoListDto(input, address));
                break;
            case MyDAOType.Managed:
                result.Add(await GetMyManagedDaoListDto(input));
                break;
        }

        return result;
    }

    // todo no hc, just bp now
    private async Task<MyDAOListDto> GetMyManagedDaoListDto(QueryMyDAOListInput input)
    {
        var bpList = await GetBPList(input.ChainId);
        if (!bpList.Contains(input.Address))
        {
            return new MyDAOListDto { Type = MyDAOType.Managed };
        }

        var managedResult = await _daoProvider.GetNetworkDAOAsync(input.ChainId);
        return await GetMyDaoListDto(MyDAOType.Managed, input.ChainId, 
            new Tuple<long, List<DAOIndex>>(1, new List<DAOIndex> { managedResult }));

    }
    
    private async Task<MyDAOListDto> GetMyOwnedDaoListDto(QueryMyDAOListInput input, string address)
    {
        var ownedResult = await _daoProvider.GetMyOwneredDAOListAsync(input, address);
        return await GetMyDaoListDto(MyDAOType.Owned, input.ChainId, ownedResult);
    }

    private async Task<MyDAOListDto> GetMyParticipatedDaoListDto(QueryMyDAOListInput input, string address)
    {
        var participatedResult = await _daoProvider.GetMyParticipatedDaoListAsync(new GetParticipatedInput
        {
            Address = address, ChainId = input.ChainId, SkipCount = input.SkipCount, MaxResultCount = input.MaxResultCount
        });
        var daoList = ObjectMapper.Map<List<IndexerDAOInfo>, List<DAOIndex>>(participatedResult.Data);
        return await GetMyDaoListDto(MyDAOType.Participated, input.ChainId,
            new Tuple<long, List<DAOIndex>>(participatedResult.TotalCount, daoList));
    }

    private async Task<MyDAOListDto> GetMyDaoListDto(MyDAOType type, string chainId, Tuple<long, List<DAOIndex>> originResult)  
    {  
        return new MyDAOListDto
        {
            Type = type, TotalCount = originResult.Item1, List = await FillDaoListAsync(chainId, originResult)
        };
    }

    private Task<ExplorerProposalResponse> GetCountTask(Common.Enum.ProposalType type)
    {
        return _explorerProvider.GetProposalPagerAsync(CommonConstant.MainChainId, new ExplorerProposalListRequest
        {
            ProposalType = type.ToString(),
            Status = "all", IsContract = 0
        });
    }
}