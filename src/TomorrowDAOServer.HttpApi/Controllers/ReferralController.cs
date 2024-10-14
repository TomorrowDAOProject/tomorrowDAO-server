using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Referral;
using TomorrowDAOServer.Referral.Dto;
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
    
    // [HttpPost("get-link")]
    // [Authorize]
    // public async Task<GetLinkDto> GetLinkAsync(GetLinkInput input)
    // {
    //     return await _referralService.GetLinkAsync(input.Token, input.ChainId);
    // }
    
    [HttpGet("invite-detail")]
    [Authorize]
    public async Task<InviteDetailDto> InviteDetailAsync(string chainId)
    {
        return await _referralService.InviteDetailAsync(chainId);
    }
    
    [HttpGet("invite-leader-board")]
    [Authorize]
    public async Task<InviteBoardPageResultDto<InviteLeaderBoardDto>> InviteLeaderBoardAsync(InviteLeaderBoardInput input)
    {
        return await _referralService.InviteLeaderBoardAsync(input);
    }
    
    [HttpGet("config")]
    public async Task<ReferralActiveConfigDto> ConfigAsync()
    {
        return await _referralService.ConfigAsync();
    }
    
    [HttpGet("referral-binding-status")]
    [Authorize]
    public async Task<ReferralBindingStatusDto> ReferralBindingStatusAsync(string chainId)
    {
        return await _referralService.ReferralBindingStatusAsync(chainId);
    }
    
    [HttpGet("set-cycle")]
    [Authorize]
    public async Task SetCycleAsync(SetCycleInput input)
    {
        await _referralService.SetCycleAsync(input);
    }
}