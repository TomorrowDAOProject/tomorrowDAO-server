using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Token.Index;
using TomorrowDAOServer.Token.Provider;
using TomorrowDAOServer.User.Provider;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace TomorrowDAOServer.Token;

public class UserTokenServiceTest
{
    private readonly IUserTokenProvider _userTokenProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ITokenProvider _tokenProvider;
    private readonly IUserProvider _userProvider;
    private readonly IUserTokenService _service;

    public UserTokenServiceTest()
    {
        _userTokenProvider = Substitute.For<IUserTokenProvider>();
        _objectMapper = Substitute.For<IObjectMapper>();
        _tokenProvider = Substitute.For<ITokenProvider>();
        _userProvider = Substitute.For<IUserProvider>();
        _service = new UserTokenService(_userTokenProvider, _objectMapper, _tokenProvider, _userProvider);
    }

    [Fact]
    public async Task GetUserTokensAsync_Test()
    {
        var result = await _service.GetUserTokensAsync("");
        result.Count.ShouldBe(0);
        
        result = await _service.GetUserTokensAsync("chainId");
        result.Count.ShouldBe(0);

        _userTokenProvider.GetUserTokens(Arg.Any<string>(), Arg.Any<string>()).Returns(new List<IndexerUserToken>
        {
            new() { TokenName = "ELF", Balance = 100000000 }, new()
        });
        _objectMapper.Map<IndexerUserToken, UserTokenDto>(Arg.Any<IndexerUserToken>()).Returns(new UserTokenDto
        {
            Symbol = "ELF", ImageUrl = ""
        });
        _tokenProvider.BuildTokenImageUrl(Arg.Any<string>()).Returns("url");
        result = await _service.GetUserTokensAsync("chainId");
        result.Count.ShouldBe(1);
    }
}