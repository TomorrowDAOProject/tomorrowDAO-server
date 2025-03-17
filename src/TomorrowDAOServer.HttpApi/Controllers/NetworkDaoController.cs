using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.NetworkDao;
using TomorrowDAOServer.NetworkDao.Dto;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using Volo.Abp;

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
    private readonly INetworkDaoVoteService _networkDaoVoteService;
    private readonly INetworkDaoOrgService _networkDaoOrgService;
    private readonly INetworkDaoContractNamesService _networkDaoContractNamesService;

    public NetworkDaoController(INetworkDaoProposalService networkDaoProposalService,
        ILogger<NetworkDaoController> logger,
        INetworkDaoTreasuryService networkDaoTreasuryService, INetworkDaoElectionService networkDaoElectionService,
        INetworkDaoVoteService networkDaoVoteService, INetworkDaoOrgService networkDaoOrgService,
        INetworkDaoContractNamesService networkDaoContractNamesService)
    {
        _networkDaoProposalService = networkDaoProposalService;
        _logger = logger;
        _networkDaoTreasuryService = networkDaoTreasuryService;
        _networkDaoElectionService = networkDaoElectionService;
        _networkDaoVoteService = networkDaoVoteService;
        _networkDaoOrgService = networkDaoOrgService;
        _networkDaoContractNamesService = networkDaoContractNamesService;
    }

    [HttpGet("proposals")]
    public async Task<GetProposalListPageResult> GetProposalListAsync(GetProposalListInput input)
    {
        return await _networkDaoProposalService.GetProposalListAsync(input);
    }

    [HttpGet("proposal/info")]
    public async Task<GetProposalInfoResultDto> GetProposalInfoAsync(GetProposalInfoInput input)
    {
        return await _networkDaoProposalService.GetProposalInfoAsync(input);
    }

    [HttpGet("proposal/applied")]
    public async Task<GetAppliedListPagedResult> GetAppliedProposalListAsync(GetAppliedListInput input)
    {
        return await _networkDaoProposalService.GetAppliedProposalListAsync(input);
    }

    [HttpGet("votes")]
    public async Task<GetVotedListPagedResult> GetVotedListAsync(GetVotedListInput input)
    {
        return await _networkDaoVoteService.GetVotedListAsync(input);
    }

    [HttpGet("vote/personal")]
    public async Task<GetAllPersonalVotesPagedResult> GetAllPersonalVotesAsync(GetAllPersonalVotesInput input)
    {
        return await _networkDaoVoteService.GetAllPersonalVotesAsync(input);
    }

    [Authorize]
    [HttpPost("vote/addTeamDesc")]
    public async Task<AddTeamDescResultDto> AddTeamDescriptionAsync(AddTeamDescInput input)
    {
        return await _networkDaoVoteService.AddTeamDescriptionAsync(input);
    }

    [Authorize]
    [HttpPost("vote/updateTeamStatus")]
    public async Task<UpdateTeamStatusResultDto> UpdateTeamStatusAsync(UpdateTeamStatusInput input)
    {
        return await _networkDaoVoteService.UpdateTeamStatusAsync(input);
    }

    [HttpGet("vote/getTeamDesc")]
    public async Task<GetTeamDescResultDto> GetTeamDescAsync(GetTeamDescInput input)
    {
        return await _networkDaoVoteService.GetTeamDescAsync(input);
    }

    [HttpGet("vote/getAllTeamDesc")]
    public async Task<List<GetTeamDescResultDto>> GetAllTeamDescAsync(GetAllTeamDescInput input)
    {
        return await _networkDaoVoteService.GetAllTeamDescAsync(input);
    }

    [HttpGet("org")]
    public async Task<GetOrganizationsPagedResult> GetOrganizationsAsync(GetOrganizationsInput input)
    {
        return await _networkDaoOrgService.GetOrganizationsAsync(input);
    }

    [HttpGet("org/owner")]
    public async Task<GetOrgOfOwnerListPagedResult> GetOrgOfOwnerListAsync(GetOrgOfOwnerListInput input)
    {
        return await _networkDaoOrgService.GetOrgOfOwnerListAsync(input);
    }

    [HttpGet("org/proposer")]
    public async Task<GetOrgOfProposerListPagedResult> GetOrgOfProposerListAsync(GetOrgOfProposerListInput input)
    {
        return await _networkDaoOrgService.GetOrgOfProposerListAsync(input);
    }

    [Obsolete]
    [HttpGet("proposal/list")]
    public async Task<ExplorerProposalResponse> GetProposalListAsync(ProposalListRequest request)
    {
        return await _networkDaoProposalService.GetProposalListAsync(request);
    }

    [Obsolete]
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

    [HttpGet("staking")]
    public async Task<long> GetBpVotingStakingAmountAsync()
    {
        return await _networkDaoElectionService.GetBpVotingStakingAmount();
    }

    [HttpPost("contract/add")]
    [Authorize]
    public async Task<AddContractNameResponse> AddContractNameAsync(AddContractNameInput input)
    {
        return await _networkDaoContractNamesService.AddContractNamesAsync(input);
    }

    [HttpPost("contract/update")]
    [Authorize]
    public async Task<UpdateContractNameResponse> UpdateContractNameAsync(UpdateContractNameInput input)
    {
        return await _networkDaoContractNamesService.UpdateContractNamesAsync(input);
    }

    [HttpGet("contract/check")]
    public async Task<CheckContractNameResponse> CheckContractNameAsync(CheckContractNameInput input)
    {
        return await _networkDaoContractNamesService.CheckContractNameAsync(input);
    }

    [Authorize]
    [HttpGet("vote/loadHistory")]
    public async Task<int> LoadVoteTeamHistoryDataAsync(LoadVoteTeamDescHistoryInput input)
    {
        return await _networkDaoVoteService.LoadVoteTeamHistoryDateAsync(input);
    }
    
    [HttpPost("contract/loadHistory")]
    [Authorize]
    public async Task<int> LoadContractHistoryDataAsync(LoadContractHistoryInput input)
    {
        return await _networkDaoContractNamesService.LoadContractHistoryDataAsync(input);
    } 
}