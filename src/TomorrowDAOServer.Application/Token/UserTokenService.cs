using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Token.Index;
using TomorrowDAOServer.Token.Provider;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Token;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserTokenService : TomorrowDAOServerAppService, IUserTokenService
{
    private readonly IUserTokenProvider _userTokenProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ITokenProvider _tokenProvider;
    private readonly IUserProvider _userProvider;
    
    public UserTokenService(IUserTokenProvider userTokenProvider, IObjectMapper objectMapper, 
        ITokenProvider tokenProvider, IUserProvider userProvider)
    {
        _userTokenProvider = userTokenProvider;
        _objectMapper = objectMapper;
        _tokenProvider = tokenProvider;
        _userProvider = userProvider;
    }

    public async Task<List<UserTokenDto>> GetUserTokensAsync(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        if (chainId.IsNullOrWhiteSpace() || address.IsNullOrWhiteSpace())
        {
            return new List<UserTokenDto>();
        }

        var list = await _userTokenProvider.GetUserTokens(chainId, address);
        return list.Where(item => item != null && item.Balance > 0)
            .Select(item =>
            {
                var userTokenDto = _objectMapper.Map<IndexerUserToken, UserTokenDto>(item);
                if (userTokenDto.ImageUrl.IsNullOrWhiteSpace())
                {
                    userTokenDto.ImageUrl = _tokenProvider.BuildTokenImageUrl(userTokenDto.Symbol);
                }
                return userTokenDto;
            })
            .ToList();
    }
}