using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking;

public partial class RankingAppServiceTest : TomorrowDaoServerApplicationTestBase
{
    private IRankingAppService _rankingAppService;
    
    public RankingAppServiceTest(ITestOutputHelper output) : base(output)
    {
        _rankingAppService = ServiceProvider.GetRequiredService<IRankingAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        //services.AddSingleton();
    }
}