using System.Threading.Tasks;
using GraphQL;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Token.Index;
using Xunit;

namespace TomorrowDAOServer.Token.Provider;

public class UserTokenProviderTest
{
    private readonly IGraphQlHelper _graphQlHelper;
    private IUserTokenProvider _provider;

    public UserTokenProviderTest()
    {
        _graphQlHelper = Substitute.For<IGraphQlHelper>();
        _provider = new UserTokenProvider(_graphQlHelper);
    }

    [Fact]
    public async Task GetUserTokens_Test()
    {
        var result = await _provider.GetUserTokens("chainId", "address");
        _graphQlHelper.QueryAsync<IndexerUserTokens>(Arg.Any<GraphQLRequest>()).Returns(new IndexerUserTokens());
        result.ShouldNotBeNull();
    }
}