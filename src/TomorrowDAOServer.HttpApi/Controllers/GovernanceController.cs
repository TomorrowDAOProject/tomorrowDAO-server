using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Governance;
using TomorrowDAOServer.Governance.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Governance")]
[Route("api/app/governance/")]
public class GovernanceController
{
    private readonly IGovernanceService _governanceService;

    public GovernanceController(IGovernanceService governanceService)
    {
        _governanceService = governanceService;
    }
    
    [HttpGet("list")]
    public async Task<GovernanceSchemeDto> GovernanceModelList(GetGovernanceSchemeListInput input)
    {
        return await _governanceService.GetGovernanceSchemeAsync(input);
    }
}