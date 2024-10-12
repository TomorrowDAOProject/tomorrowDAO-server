using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Token")]
[Route("api/app/token")]
public class TokenController
{
    private readonly ITokenService _tokenService;
    private readonly ITransferTokenService _transferTokenService;
    private readonly IIssueTokenService _issueTokenService;

    public TokenController(ITokenService tokenService, ITransferTokenService transferTokenService, IIssueTokenService issueTokenService)
    {
        _tokenService = tokenService;
        _transferTokenService = transferTokenService;
        _issueTokenService = issueTokenService;
    }

    [HttpGet]
    public async Task<TokenInfoDto> GetTokenAsync(GetTokenInput input)
    {
        return await _tokenService.GetTokenInfoAsync(input.ChainId, input.Symbol);
    }

    [HttpGet]
    [Route("price")]
    public async Task<TokenPriceDto> GetTokenPriceAsync(GetTokenPriceInput input)
    {
        return await _tokenService.GetTokenPriceAsync(input.BaseCoin, input.QuoteCoin);
    }

    [HttpGet]
    [Route("tvl")]
    public async Task<TvlDetail> GetTokenPriceAsync(string chainId)
    {
        return await _tokenService.GetTvlAsync(chainId);
    }

    [HttpPost("transfer")]
    [Authorize]
    public async Task<TransferTokenResponse> TransferTokenAsync(TransferTokenInput input)
    {
        return await _transferTokenService.TransferTokenAsync(input);
    }

    [HttpPost("transfer/status")]
    public async Task<TokenClaimRecord> GetTransferTokenStatusAsync(TransferTokenStatusInput input)
    {
        return await _transferTokenService.GetTransferTokenStatusAsync(input);
    }

    [HttpPost("issue")]
    public async Task<IssueTokenResponse> IssueTokensAsync(IssueTokensInput input)
    {
        return await _issueTokenService.IssueTokensAsync(input);
    }
}