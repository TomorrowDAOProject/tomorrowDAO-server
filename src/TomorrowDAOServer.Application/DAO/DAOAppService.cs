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
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.DAO;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class DAOAppService : ApplicationService, IDAOAppService
{
    private readonly IDAOProvider _daoProvider;
    private readonly IElectionProvider _electionProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IOptionsMonitor<DaoOptions> _testDaoOptions;
    private readonly IGovernanceProvider _governanceProvider;
    private readonly IContractProvider _contractProvider;
    private const int ZeroSkipCount = 0;
    private const int GetMemberListMaxResultCount = 100;
    private const int CandidateTermNumber = 0;
    private ValueTuple<long, long> ProposalCountCache = new(0, 0);

    public DAOAppService(IDAOProvider daoProvider, IElectionProvider electionProvider,
        IGovernanceProvider governanceProvider,
        IProposalProvider proposalProvider, IExplorerProvider explorerProvider, IGraphQLProvider graphQlProvider,
        IObjectMapper objectMapper, IOptionsMonitor<DaoOptions> testDaoOptions, IContractProvider contractProvider)
    {
        _daoProvider = daoProvider;
        _electionProvider = electionProvider;
        _proposalProvider = proposalProvider;
        _graphQlProvider = graphQlProvider;
        _objectMapper = objectMapper;
        _testDaoOptions = testDaoOptions;
        _contractProvider = contractProvider;
        _explorerProvider = explorerProvider;
        _governanceProvider = governanceProvider;
    }

    public async Task<DAOInfoDto> GetDAOByIdAsync(GetDAOInfoInput input)
    {
        var daoIndex = await _daoProvider.GetAsync(input);
        if (daoIndex == null)
        {
            return new DAOInfoDto();
        }

        input.DAOId = daoIndex.Id;
        
        var getTreasuryAddressTask = _contractProvider.GetTreasuryAddressAsync(input.ChainId, input.DAOId);
        var getHighCouncilMembersTask = _electionProvider.GetHighCouncilMembersAsync(input.ChainId, input.DAOId);

        var daoInfo = _objectMapper.Map<DAOIndex, DAOInfoDto>(daoIndex);
        var governanceScheme = (await _governanceProvider.GetGovernanceSchemeAsync(input.ChainId, input.DAOId)).Data;
        daoInfo.OfGovernanceSchemeThreshold(governanceScheme.FirstOrDefault());
        await getTreasuryAddressTask;
        daoInfo.TreasuryAccountAddress = getTreasuryAddressTask.Result;
        if (daoInfo.TreasuryContractAddress.IsNullOrWhiteSpace())
        {
            daoInfo.TreasuryContractAddress =
                _contractProvider.ContractAddress(input.ChainId, CommonConstant.TreasuryContractAddressName);
        }

        if (!daoInfo.IsNetworkDAO)
        {
            await getHighCouncilMembersTask;
            daoInfo.HighCouncilMemberCount = getHighCouncilMembersTask.Result.IsNullOrEmpty()
                ? 0
                : getHighCouncilMembersTask.Result.Count;
            return daoInfo;
        }

        var bpInfo = await _graphQlProvider.GetBPWithRoundAsync(input.ChainId);
        daoInfo.HighCouncilTermNumber = bpInfo.Round;
        daoInfo.HighCouncilMemberCount = bpInfo.AddressList.Count;
        return daoInfo;
    }

    public async Task<PagedResultDto<HcMemberDto>> GetHcMemberListAsync(GetHcMemberInput input)
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

    public async Task<PageResultDto<MemberDto>> GetMemberListAsync(GetMemberListInput listInput)
    {
        return await _daoProvider.GetMemberListAsync(listInput);
    }

    public async Task<PagedResultDto<DAOListDto>> GetDAOListAsync(QueryDAOListInput input)
    {
        var daoOption = _testDaoOptions.CurrentValue;
        var begin = input.SkipCount;
        var end = begin + input.MaxResultCount;
        var topCount = daoOption.TopDaoNames.Count;
        var excludeNames = new HashSet<string>(daoOption.FilteredDaoNames.Union(daoOption.TopDaoNames));
        Tuple<long, List<DAOListDto>> normalSearch;
        if (begin >= topCount)
        {
            input.SkipCount -= topCount;
            normalSearch = await GetNormalSearchList(input, excludeNames);
            return new PagedResultDto<DAOListDto>
            {
                TotalCount = topCount + normalSearch.Item1,
                Items = normalSearch.Item2
            };
        }

        Tuple<long, List<DAOListDto>> nameSearch;
        if (end <= topCount)
        {
            nameSearch = await GetNameSearchList(input, daoOption.TopDaoNames.Skip(begin).Take(end - begin).ToList());
            var normalCount = await _daoProvider.GetDAOListCountAsync(input, excludeNames);
            return new PagedResultDto<DAOListDto>
            {
                TotalCount = topCount + normalCount,
                Items = nameSearch.Item2
            };
        }

        nameSearch = await GetNameSearchList(input, daoOption.TopDaoNames.Skip(begin).Take(topCount - begin).ToList());
        input.SkipCount = 0;
        input.MaxResultCount = end - topCount;
        normalSearch = await GetNormalSearchList(input, excludeNames);
        var combineList = new List<DAOListDto>();
        combineList.AddRange(nameSearch.Item2);
        combineList.AddRange(normalSearch.Item2);
        return new PagedResultDto<DAOListDto>
        {
            TotalCount = topCount + normalSearch.Item1,
            Items = combineList
        };
    }

    private async Task<Tuple<long, List<DAOListDto>>> GetNormalSearchList(QueryDAOListInput input,
        ISet<string> excludeNames)
    {
        return await FillDaoListAsync(input.ChainId,
            await _daoProvider.GetDAOListAsync(input, excludeNames));
    }

    private async Task<Tuple<long, List<DAOListDto>>> GetNameSearchList(QueryDAOListInput input,
        List<string> searchNames)
    {
        var result = await FillDaoListAsync(input.ChainId,
            await _daoProvider.GetDAOListByNameAsync(input.ChainId, searchNames));
        return new Tuple<long, List<DAOListDto>>(result.Item1,
            result.Item2.OrderBy(x => searchNames.IndexOf(x.Name)).ToList());
    }

    private async Task<Tuple<long, List<DAOListDto>>> FillDaoListAsync(string chainId,
        Tuple<long, List<DAOIndex>> originResult)
    {
        var daoList = originResult.Item2;
        var items = _objectMapper.Map<List<DAOIndex>, List<DAOListDto>>(daoList);
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

        return new Tuple<long, List<DAOListDto>>(originResult.Item1, items);
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
            Address = address, ChainId = input.ChainId, SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        });
        if (participatedResult.Data.IsNullOrEmpty())
        {
            return new MyDAOListDto();
        }
        
        var daoIds = participatedResult.Data.Select(participated => participated.Id).ToList();
        var daoList = await _daoProvider.GetDaoListByDaoIds(input.ChainId, daoIds);
        //var daoList = _objectMapper.Map<List<IndexerDAOInfo>, List<DAOIndex>>(participatedResult.Data);
        return await GetMyDaoListDto(MyDAOType.Participated, input.ChainId,
            new Tuple<long, List<DAOIndex>>(participatedResult.TotalCount, daoList));
    }

    private async Task<MyDAOListDto> GetMyDaoListDto(MyDAOType type, string chainId,
        Tuple<long, List<DAOIndex>> originResult)
    {
        return new MyDAOListDto
        {
            Type = type, TotalCount = originResult.Item1, List = (await FillDaoListAsync(chainId, originResult)).Item2
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