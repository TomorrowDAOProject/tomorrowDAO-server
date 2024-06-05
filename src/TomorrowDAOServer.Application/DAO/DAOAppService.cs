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
        return await FillDAOListAsync(input.ChainId,
            await _daoProvider.GetDAOListAsync(input, excludeNames));
    }

    private async Task<List<DAOListDto>> GetNameSearchList(QueryDAOListInput input, List<string> searchNames)
    {
        return (await FillDAOListAsync(input.ChainId,
                await _daoProvider.GetDAOListByNameAsync(input.ChainId, searchNames)))
            .OrderBy(x => searchNames.IndexOf(x.Name)).ToList();
    }

    public async Task<List<DAOListDto>> FillDAOListAsync(string chainId, Tuple<long, List<DAOIndex>> originResult)
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
        var result = new List<MyDAOListDto>();
        var address = await _userService.GetCurrentUserAddressAsync(input.ChainId);
        if (address.IsNullOrEmpty())
        {
            return result;
        }

        switch (input.Type)
        {
            case MyDAOType.Managed:
                // todo no hc currently, just get bp
                var bpList = await _graphQlProvider.GetBPAsync(input.ChainId);
                if (bpList.Contains(address))
                {
                    result.Add(new MyDAOListDto
                    {
                        Type = MyDAOType.Managed,
                        List = new List<DAOListDto> { ObjectMapper.Map<DAOIndex, DAOListDto>(await _daoProvider.GetNetworkDAOAsync(input.ChainId)) }
                    });
                }
                break;
            case MyDAOType.Owned:
                result.Add( new MyDAOListDto
                {
                    Type = MyDAOType.Owned,
                    List = ObjectMapper.Map<List<DAOIndex>, List<DAOListDto>>((await _daoProvider.GetMyOwneredDAOListAsync(input, address)).Item2)
                });
                break;
            case MyDAOType.Participated:
        }

        return result;
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