using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.NetworkDao;
using TomorrowDAOServer.NetworkDao.Dto;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Network dao")]
[Route("api/app/networkdao")]
public class NetworkDaoController
{
    private ILogger<NetworkDaoController> _logger;
    private readonly IProposalService _proposalService;
    private readonly ITreasuryService _treasuryService;
    private readonly IElectionService _electionService;

    public NetworkDaoController(IProposalService proposalService, ILogger<NetworkDaoController> logger,
        ITreasuryService treasuryService, IElectionService electionService)
    {
        _proposalService = proposalService;
        _logger = logger;
        _treasuryService = treasuryService;
        _electionService = electionService;
    }
    
    [HttpGet("proposal/list")]
    public async Task<ExplorerProposalResponse> GetProposalList(ProposalListRequest request)
    {
        return await _proposalService.GetProposalListAsync(request);
    }

    [HttpGet("proposal/detail")]
    public async Task<NetworkDaoProposalDto> GetProposalInfo(ProposalInfoRequest request)
    {
        return await _proposalService.GetProposalInfoAsync(request);
    }

    [HttpGet("proposal/home-page")]
    public async Task<HomePageResponse> ProposalHomePage(HomePageRequest homePageRequest)
    {
        return await _proposalService.GetHomePageAsync(homePageRequest);
    }
    
    [HttpGet("treasury/balance")]
    public async Task<TreasuryBalanceResponse> TreasuryBalance(TreasuryBalanceRequest request)
    {
        return await _treasuryService.GetBalanceAsync(request);
    }

    [HttpGet("treasury/transactions-records")]
    public async Task<PagedResultDto<TreasuryTransactionDto>> TreasuryTransactionRecords(
        TreasuryTransactionRequest request)
    {
        return await _treasuryService.GetTreasuryTransactionAsync(request);
    }

    [HttpGet("staking")]
    public async Task<long> GetBpVotingStakingAmount()
    {
        return await _electionService.GetBpVotingStakingAmount();
    }
}