using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Election;
using TomorrowDAOServer.Election.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Election")]
[Route("api/app/council")]
public class ElectionController : AbpController
{
    private readonly IElectionService _electionService;

    public ElectionController(IElectionService electionService)
    {
        _electionService = electionService;
    }

    [HttpPost("members")]
    public async Task<List<string>> GetHighCouncilMembersAsync(HighCouncilMembersInput input)
    {
        return await _electionService.GetHighCouncilMembersAsync(input);
    }
}