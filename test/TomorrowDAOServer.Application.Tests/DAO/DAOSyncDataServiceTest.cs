using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.DAO;

public partial class DAOSyncDataServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly DAOSyncDataService _daoSyncDataService;

    public DAOSyncDataServiceTest(ITestOutputHelper output) : base(output)
    {
        _daoSyncDataService = Application.ServiceProvider.GetRequiredService<DAOSyncDataService>();
    }

    [Fact]
    public async Task SyncIndexerRecordsAsyncTest()
    {
        var hight = await _daoSyncDataService.SyncIndexerRecordsAsync(ChainIdAELF, 0, 10000);
        hight.ShouldBe(10000);
    }

    [Fact]
    public async Task GetChainIdsAsyncTest()
    {
        var chainIds = await _daoSyncDataService.GetChainIdsAsync();
        chainIds.ShouldNotBeNull();
        chainIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetWorkerBusinessType()
    {
        var businessType = _daoSyncDataService.GetBusinessType();
        businessType.ShouldBe(WorkerBusinessType.DAOSync);
    }
}