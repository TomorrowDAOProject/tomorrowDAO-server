using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Provider;
using TomorrowDAOServer.Providers;
using Xunit;

namespace TomorrowDAOServer.Proposal;

public class ProposalNumUpdateServiceTest
{
    private readonly ILogger<ProposalNumUpdateService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IScheduleSyncDataService _service;
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;

    public ProposalNumUpdateServiceTest()
    {
        _logger = Substitute.For<ILogger<ProposalNumUpdateService>>();
        _chainAppService = Substitute.For<IChainAppService>();
        _explorerProvider = Substitute.For<IExplorerProvider>();
        _graphQlProvider = Substitute.For<IGraphQLProvider>();
        _networkDaoEsDataProvider = Substitute.For<INetworkDaoEsDataProvider>();
        _service = new ProposalNumUpdateService(_logger, _graphQlProvider, _chainAppService, _explorerProvider, _networkDaoEsDataProvider);
    }
    
    [Fact]
    public async Task SyncIndexerRecordsAsync_Test()
    {
        // _explorerProvider.GetProposalPagerAsync(Arg.Any<string>(), Arg.Any<ExplorerProposalListRequest>())
        //     .Returns(new ExplorerProposalResponse { Total = 2 });
        // var result = await _service.SyncIndexerRecordsAsync("tDVW", 0, 0);
        // result.ShouldBe(-1);
    }

    [Fact]
    public async Task GetChainIdsAsync_Test()
    {
        _chainAppService.GetListAsync().Returns(new[] { "tDVW" });
        var result = await _service.GetChainIdsAsync();
        result.Count.ShouldBe(1);
        result[0].ShouldBe("tDVW");
    }

    [Fact]
    public void GetBusinessType_Test()
    {
        var result = _service.GetBusinessType();
        result.ShouldBe(WorkerBusinessType.ProposalNumUpdate);
    }
}