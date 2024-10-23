using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Election;

public partial class BPInfoUpdateServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly BPInfoUpdateService _bpInfoUpdateService;
    
    public BPInfoUpdateServiceTest(ITestOutputHelper output) : base(output)
    {
        _bpInfoUpdateService = Application.ServiceProvider.GetRequiredService<BPInfoUpdateService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(ScriptServiceTest.MockTransactionService());
    }
    
    

    [Fact]
    public async Task SyncIndexerRecordsAsyncTest()
    {
        var records = await _bpInfoUpdateService.SyncIndexerRecordsAsync(ChainIdAELF, 0, 1000);
        records.ShouldBe(1000);
    }

    [Fact]
    public async Task GetChainIdsAsyncTest()
    {
        var chainIds = await _bpInfoUpdateService.GetChainIdsAsync();
        chainIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetBusinessTypeTest()
    {
        var businessType = _bpInfoUpdateService.GetBusinessType();
        businessType.ShouldBe(WorkerBusinessType.BPInfoUpdate);
    }
}