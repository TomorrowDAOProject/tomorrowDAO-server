using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.NetworkDao;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Network dao")]
[Route("api/app")]
public class NetworkDaoController
{
    private ILogger<NetworkDaoController> _logger;
    private readonly IProposalService _proposalService;
    private readonly ITreasuryService _treasuryService;

    public NetworkDaoController(IProposalService proposalService, ILogger<NetworkDaoController> logger,
        ITreasuryService treasuryService)
    {
        _proposalService = proposalService;
        _logger = logger;
        _treasuryService = treasuryService;
    }

    [HttpGet("proposal/home-page")]
    public async Task<HomePageResponse> ProposalHomePage(HomePageRequest homePageRequest)
    {
        return await _proposalService.GetHomePageAsync(homePageRequest);
    }

    [HttpGet("proposal/list")]
    public async Task<PagedResultDto<ProposalListResponse>> ProposalList(ProposalListRequest request)
    {
        return await _proposalService.GetProposalList(request);
    }
    
    [HttpGet("treasury/balance")]
    public async Task<TreasuryBalanceResponse> TreasuryBalance(TreasuryBalanceRequest request)
    {
        return await _treasuryService.GetBalanceAsync(request);
    }

    [HttpGet("treasury/transactions-records")]
    public async Task<PagedResultDto<TreasuryTransactionDto>> TreasuryTransactionRecords(TreasuryTransactionRequest request)
    {
        return await _treasuryService.GetTreasuryTransactionAsync(request);
    }

}