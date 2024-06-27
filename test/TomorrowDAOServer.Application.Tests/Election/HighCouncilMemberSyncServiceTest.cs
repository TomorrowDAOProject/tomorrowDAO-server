using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Governance;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Election;

public partial class HighCouncilMemberSyncServiceTest : TomorrowDaoServerApplicationTestBase
{

    private readonly HighCouncilMemberSyncService _highCouncilMemberSyncService;
    
    public HighCouncilMemberSyncServiceTest(ITestOutputHelper output) : base(output)
    {
        _highCouncilMemberSyncService = Application.ServiceProvider.GetRequiredService<HighCouncilMemberSyncService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockGraphQlHelper_QueryElectionDto());
        services.AddSingleton(MockTransactionService());
        services.AddSingleton(MockQueryContractOption());
        services.AddSingleton(MockHighCouncilMembersGrain());
        services.AddSingleton(MockGraphQLProvider());
    }
    
    [Fact]
    public async void SyncIndexerRecordsAsync_Test()
    {
        var blockHeight = await _highCouncilMemberSyncService.SyncIndexerRecordsAsync(ChainIdAELF, 0, 10000);
        blockHeight.ShouldBe(10000);
    }
}