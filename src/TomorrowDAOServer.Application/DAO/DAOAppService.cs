using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
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
    private const int ZeroSkipCount = 0;
    private const int GetMemberListMaxResultCount = 100;
    private const int CandidateTermNumber = 0;
    private ValueTuple<long, long> ProposalCountCache = new(0, 0);
    
    public DAOAppService(IDAOProvider daoProvider,
        IElectionProvider electionProvider,
        IProposalProvider proposalProvider, IExplorerProvider explorerProvider,
        IGraphQLProvider graphQlProvider, IVoteProvider voteProvider)
    {
        _daoProvider = daoProvider;
        _electionProvider = electionProvider;
        _proposalProvider = proposalProvider;
        _graphQlProvider = graphQlProvider;
        _voteProvider = voteProvider;
        _explorerProvider = explorerProvider;
    }

    public async Task<DAOInfoDto> GetDAOByIdAsync(GetDAOInfoInput input)
    {
        var daoIndex = await _daoProvider.GetAsync(input);
        var daoInfo = ObjectMapper.Map<DAOIndex, DAOInfoDto>(daoIndex);
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
        var (item1, daoList) = await _daoProvider.GetDAOListAsync(input);
        var items = ObjectMapper.Map<List<DAOIndex>, List<DAOListDto>>(daoList);
        var symbols = items.Select(x => x.Symbol.ToUpper()).Distinct().ToList();
        var tokenInfos = new Dictionary<string, TokenInfoDto>();
        foreach (var symbol in symbols)
        {
            tokenInfos[symbol] = await _explorerProvider.GetTokenInfoAsync(input.ChainId, symbol);
        }
        
        foreach (var dao in items)
        {
            if (!dao.Symbol.IsNullOrEmpty())
            {
                dao.SymbolHoldersNum = tokenInfos.TryGetValue(dao.Symbol.ToUpper(), out var tokenInfo) ? long.Parse(tokenInfo.Holders) : 0L;
            }

            dao.ProposalsNum = await _proposalProvider.GetProposalCountByDAOIds(input.ChainId, dao.DaoId);
            if (!dao.IsNetworkDAO)
            {
                continue;
            }

            dao.HighCouncilMemberCount = (await _graphQlProvider.GetBPAsync(input.ChainId)).Count;
            if (DateTime.UtcNow.ToUtcMilliSeconds() - ProposalCountCache.Item2 >= 10 * 60 * 1000)
            {
                var parliamentTask = GetCountTask(Common.Enum.ProposalType.Parliament);
                var associationTask = GetCountTask(Common.Enum.ProposalType.Association);
                var referendumTask = GetCountTask(Common.Enum.ProposalType.Referendum);
                await Task.WhenAll(parliamentTask, associationTask, referendumTask);
                dao.ProposalsNum += (await parliamentTask).Total + (await associationTask).Total + (await referendumTask).Total;
                ProposalCountCache = new ValueTuple<long, long>(dao.ProposalsNum, DateTime.UtcNow.ToUtcMilliSeconds());
            }
            else
            {
                dao.ProposalsNum += ProposalCountCache.Item1;
            }
        }

        return new PagedResultDto<DAOListDto>
        {
            TotalCount = 0,
            Items = items
        };
    }

    public async Task<List<string>> GetBPList(string chainId)
    {
        return await _graphQlProvider.GetBPAsync(chainId);
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