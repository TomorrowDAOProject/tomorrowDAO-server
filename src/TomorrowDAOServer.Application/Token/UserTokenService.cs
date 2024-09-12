using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Token.Index;
using TomorrowDAOServer.Token.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Token;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserTokenService : TomorrowDAOServerAppService, IUserTokenService
{
    private readonly IUserTokenProvider _userTokenProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ITokenProvider _tokenProvider;
    
    public UserTokenService(IUserTokenProvider userTokenProvider, IObjectMapper objectMapper, 
        ITokenProvider tokenProvider)
    {
        _userTokenProvider = userTokenProvider;
        _objectMapper = objectMapper;
        _tokenProvider = tokenProvider;
    }

    public async Task<List<UserTokenDto>> GetUserTokensAsync(string chainId, string address)
    {
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