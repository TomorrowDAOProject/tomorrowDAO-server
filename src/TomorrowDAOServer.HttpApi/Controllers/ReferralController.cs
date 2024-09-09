using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Referral;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Referral")]
[Route("api/app/referral")]
public class ReferralController
{
    private readonly IReferralService _referralService;

    public ReferralController(IReferralService referralService)
    {
        _referralService = referralService;
    }
    
    [HttpGet("get-link")]
    [Authorize]
    public async Task<string> GetLinkAsync(string token, string chainId)
    {
        return await _referralService.GetLinkAsync(token, chainId);
    }
}