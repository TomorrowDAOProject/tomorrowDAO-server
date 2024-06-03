using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Dto;
using TomorrowDAOServer.User;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;


[RemoteService]
[Area("app")]
[ControllerName("Token")]
[Route("api/app/token")]
public class TokenController
{
    private readonly IUserTokenService _userTokenService;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public TokenController(IUserTokenService userTokenService, IUserService userService, ITokenService tokenService)
    {
        _userTokenService = userTokenService;
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<TokenDto> GetTokenAsync(GetTokenInput input)
    {
        return await _tokenService.GetTokenByExplorerAsync(input.ChainId, input.Symbol);
    }

    [HttpGet]
    [Route("list")]
    [Authorize]
    public async Task<List<UserTokenDto>> GetTokenAsync(GetUserTokenInput input)
    {
        var userAddress = await _userService.GetCurrentUserAddressAsync(input.ChainId);
        return await _userTokenService.GetUserTokensAsync(input.ChainId, userAddress);
    }
    
    [HttpGet]
    [Route("price")]
    public async Task<TokenPriceDto> GetTokenPriceAsync(GetTokenPriceInput input)
    {
        return await _tokenService.GetTokenPriceAsync(input.BaseCoin, input.QuoteCoin);
    }

    [HttpGet]
    [Route("tvl")]
    public async Task<double> GetTokenPriceAsync(string chainId)
    {
        return await _tokenService.GetTvlAsync(chainId);
    }

}