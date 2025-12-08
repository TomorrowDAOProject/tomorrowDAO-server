using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common.GraphQL;
using Xunit;

namespace TomorrowDAOServer.Governance.Provider;

public class GovernanceProviderTest
{
    private readonly GovernanceProvider _governanceProvider =
        new(Substitute.For<IGraphQlHelper>(), Substitute.For<ILogger<GovernanceProvider>>());


    [Fact]
    public async void GetGovernanceMechanismAsync_Test()
    {
        var result = await _governanceProvider.GetGovernanceSchemeAsync("AELF", "aaa");
        result.ShouldNotBeNull();
    }
}