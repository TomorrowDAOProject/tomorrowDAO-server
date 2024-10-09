using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.NetworkDao;
using TomorrowDAOServer.NetworkDao.Dto;
using TomorrowDAOServer.NetworkDao.Migrator;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
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
    private readonly INetworkDaoProposalService _networkDaoProposalService;
    private readonly INetworkDaoTreasuryService _networkDaoTreasuryService;
    private readonly INetworkDaoElectionService _networkDaoElectionService;

    public NetworkDaoController(INetworkDaoProposalService networkDaoProposalService, ILogger<NetworkDaoController> logger,
        INetworkDaoTreasuryService networkDaoTreasuryService, INetworkDaoElectionService networkDaoElectionService)
    {
        _networkDaoProposalService = networkDaoProposalService;
        _logger = logger;
        _networkDaoTreasuryService = networkDaoTreasuryService;
        _networkDaoElectionService = networkDaoElectionService;
    }

    [HttpGet("proposal")]
    public async Task<GetProposalListPageResult> GetProposalListAsync(GetProposalListInput input)
    {
        return await _networkDaoProposalService.GetProposalListAsync(input);
    }

    [HttpGet("proposal/proposalInfo")]
    public async Task<GetProposalInfoResultDto> GetProposalInfoAsync(GetProposalInfoInput input)
    {
        return await _networkDaoProposalService.GetProposalInfoAsync(input);
    }

    [Obsolete]
    [HttpGet("proposal/list")]
    public async Task<ExplorerProposalResponse> GetProposalListAsync(ProposalListRequest request)
    {
        return await _networkDaoProposalService.GetProposalListAsync(request);
    }

    [HttpGet("proposal/detail")]
    public async Task<NetworkDaoProposalDto> GetProposalInfoAsync(ProposalInfoRequest request)
    {
        return await _networkDaoProposalService.GetProposalInfoAsync(request);
    }

    [HttpGet("proposal/home-page")]
    public async Task<HomePageResponse> ProposalHomePageAsync(HomePageRequest homePageRequest)
    {
        return await _networkDaoProposalService.GetHomePageAsync(homePageRequest);
    }
    
    [HttpGet("treasury/balance")]
    public async Task<TreasuryBalanceResponse> TreasuryBalanceAsync(TreasuryBalanceRequest request)
    {
        return await _networkDaoTreasuryService.GetBalanceAsync(request);
    }

    [HttpGet("treasury/transactions-records")]
    public async Task<PagedResultDto<TreasuryTransactionDto>> TreasuryTransactionRecordsAsync(
        TreasuryTransactionRequest request)
    {
        return await _networkDaoTreasuryService.GetTreasuryTransactionAsync(request);
    }

    [HttpGet("staking")]
    public async Task<long> GetBpVotingStakingAmountAsync()
    {
        return await _networkDaoElectionService.GetBpVotingStakingAmount();
    }
}