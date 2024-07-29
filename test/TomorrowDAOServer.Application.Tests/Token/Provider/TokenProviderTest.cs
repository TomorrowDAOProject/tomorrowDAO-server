using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Options;
using Xunit;

namespace TomorrowDAOServer.Token.Provider;

public class TokenProviderTest
{
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenInfoOptionsMonitor;
    private readonly IOptionsMonitor<AssetsInfoOptions> _assetsInfoOptionsMonitor;
    private readonly ITokenProvider _provider;

    public TokenProviderTest()
    {
        _tokenInfoOptionsMonitor = Substitute.For<IOptionsMonitor<TokenInfoOptions>>();
        _assetsInfoOptionsMonitor = Substitute.For<IOptionsMonitor<AssetsInfoOptions>>();
        _provider = new TokenProvider(_tokenInfoOptionsMonitor, _assetsInfoOptionsMonitor);
    }

    [Fact]
    public async Task BuildTokenImageUrl_Test()
    {
        var result = _provider.BuildTokenImageUrl("");
        result.ShouldBe(string.Empty);

        _assetsInfoOptionsMonitor.CurrentValue.Returns(new AssetsInfoOptions());
        _tokenInfoOptionsMonitor.CurrentValue.Returns(new TokenInfoOptions
        {
            TokenInfos = new Dictionary<string, TokenInfo>()
        });
        result = _provider.BuildTokenImageUrl("ELF");
        result.ShouldBe(string.Empty);
        
        _assetsInfoOptionsMonitor.CurrentValue.Returns(new AssetsInfoOptions
        {
            ImageUrlPrefix = "prefix", ImageUrlSuffix = "suffix"
        });
        result = _provider.BuildTokenImageUrl("ELF");
        result.ShouldBe("prefixELFsuffix");
        
        _tokenInfoOptionsMonitor.CurrentValue.Returns(new TokenInfoOptions
        {
            TokenInfos = new Dictionary<string, TokenInfo>
            {
                {"ELF", new TokenInfo{ ImageUrl = "url"}}
            }
        });
        result = _provider.BuildTokenImageUrl("ELF");
        result.ShouldBe("url");
    }
}