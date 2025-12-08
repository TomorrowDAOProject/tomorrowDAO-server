using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Vote;
using TomorrowDAOServer.Vote.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Vote")]
[Route("api/app/vote/")]
public class VoteController
{
    private readonly IVoteService _voteService;

    public VoteController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    [HttpGet("vote-scheme-list")]
    public async Task<VoteSchemeDetailDto> VoteSchemeList(GetVoteSchemeInput input)
    {
        return await _voteService.GetVoteSchemeAsync(input);
    }
}