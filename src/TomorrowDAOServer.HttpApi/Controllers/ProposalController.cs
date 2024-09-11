using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Proposal;
using TomorrowDAOServer.Proposal.Dto;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Proposal")]
[Route("api/app/proposal")]
public class ProposalController : AbpController
{
    private readonly IProposalService _proposalService;
    private readonly ILogger<ProposalController> _logger;

    public ProposalController(IProposalService proposalService, ILogger<ProposalController> logger)
    {
        _proposalService = proposalService;
        _logger = logger;
    }

    [HttpPost("list")]
    public async Task<ProposalPagedResultDto<ProposalDto>> QueryProposalListAsync(QueryProposalListInput input)
    {
        return await _proposalService.QueryProposalListAsync(input);
    }
    
    [HttpGet]
    [Route("detail")]
    public async Task<ProposalDetailDto> QueryProposalDetailAsync(QueryProposalDetailInput input)
    {
        var sw = Stopwatch.StartNew();
        var result = await _proposalService.QueryProposalDetailAsync(input);
        sw.Stop();
        _logger.LogInformation("ProposalController QueryProposalDetailAsync duration:{0}", sw.ElapsedMilliseconds);
        return result;
    }
    
    [HttpGet("my-info")]
    [Authorize]
    public async Task<MyProposalDto> QueryMyInfoAsync(QueryMyProposalInput input)
    {
        var sw = Stopwatch.StartNew();
        
        var result = await _proposalService.QueryMyInfoAsync(input);
        
        sw.Stop();
        _logger.LogInformation("ProposalController QueryMyInfoAsync duration:{0}", sw.ElapsedMilliseconds);
        
        return result;
    }
    
    [HttpGet("vote-history")]
    // [Authorize]
    public async Task<VoteHistoryPagedResultDto<IndexerVoteHistoryDto>> QueryVoteHistoryAsync(QueryVoteHistoryInput input)
    {
        var sw = Stopwatch.StartNew();
        
        var result = await _proposalService.QueryVoteHistoryAsync(input);
        
        sw.Stop();
        _logger.LogInformation("ProposalController QueryVoteHistoryAsync duration:{0}", sw.ElapsedMilliseconds);
        
        return result;
    }

    [HttpGet("executable-list")]
    public async Task<ProposalPagedResultDto<ProposalBasicDto>> QueryExecutableProposalsAsync(QueryExecutableProposalsInput input)
    {
        var sw = Stopwatch.StartNew();
        
        var result = await _proposalService.QueryExecutableProposalsAsync(input);
        
        sw.Stop();
        _logger.LogInformation("ProposalController QueryExecutableProposalsAsync duration:{0}", sw.ElapsedMilliseconds);
        
        return result;
    }
}