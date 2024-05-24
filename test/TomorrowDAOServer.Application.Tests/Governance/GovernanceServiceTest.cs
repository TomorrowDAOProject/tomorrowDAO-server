using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Governance;

public partial class GovernanceServiceTest : TomorrowDaoServerApplicationTestBase
{ 
    private readonly IGovernanceService _governanceService;
    
    public GovernanceServiceTest(ITestOutputHelper output) : base(output)
    {
        _governanceService = Application.ServiceProvider.GetRequiredService<IGovernanceService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        //services.AddSingleton(MockGraphQlHelper());
        services.AddSingleton(MockGraphQlHelper_QueryIndexerGovernanceSchemeDto());
    }
    
    [Fact]
    public async void GetGovernanceMechanismAsync_Test()
    {
        var input = new GetGovernanceSchemeListInput
        {
            ChainId = "AELF",
            DaoId = "aa"
        };
        var result = await _governanceService.GetGovernanceSchemeAsync(input);
        result.ShouldNotBeNull();
        result.Data.Count.ShouldBe(2);
    }
}