// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Shouldly;
// using TomorrowDAOServer.Chains;
// using TomorrowDAOServer.Common.Provider;
// using TomorrowDAOServer.Entities;
// using TomorrowDAOServer.Enums;
// using TomorrowDAOServer.Proposal.Provider;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace TomorrowDAOServer.Proposal;
//
// public sealed class ProposalUpdateServiceTest : TomorrowDAOServerApplicationTestBase
// {
//     private Mock<ILogger<ProposalSyncDataService>> _loggerMock;
//     private ProposalUpdateService _proposalUpdateService;
//     private Mock<IProposalProvider> _proposalProviderMock;
//     private Mock<IChainAppService> _chainAppServiceMock;
//
//     public ProposalUpdateServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
//     {
//         _loggerMock = new Mock<ILogger<ProposalSyncDataService>>();
//         _proposalProviderMock = new Mock<IProposalProvider>();
//         _chainAppServiceMock = new Mock<IChainAppService>();
//         _proposalUpdateService = new ProposalUpdateService(
//             _loggerMock.Object,
//             It.IsAny<IGraphQLProvider>(),
//             _proposalProviderMock.Object,
//             _chainAppServiceMock.Object);
//     }
//     
//     [Fact]
//     public async Task SyncIndexerRecordsAsync_ShouldSyncExpiredProposals_AndReturnBlockHeight()
//     {
//         // Arrange
//         var proposalIndexList = new List<ProposalIndex>
//         {
//             new ProposalIndex { ProposalId1 = "1", ProposalStatus = ProposalStatus.Approved, BlockHeight = 100 },
//             new ProposalIndex { ProposalId1 = "2", ProposalStatus = ProposalStatus.Approved, BlockHeight = 200 }
//         };
//
//         _proposalProviderMock.Setup(x => x.GetNonFinishedProposalListAsync(It.Is<int>(p => p == 0),It.IsAny<List<ProposalStatus>>()))
//             .ReturnsAsync(proposalIndexList);
//         _proposalProviderMock.Setup(x => x.GetNonFinishedProposalListAsync(It.Is<int>(p => p > 0),It.IsAny<List<ProposalStatus>>()))
//             .ReturnsAsync(new List<ProposalIndex>());
//
//         // Act
//         var blockHeight = await _proposalUpdateService.SyncIndexerRecordsAsync(ChainIdTDVV, 0, 1000);
//
//         // Assert
//         _proposalProviderMock.Verify(x => x.BulkAddOrUpdateAsync(It.IsAny<List<ProposalIndex>>()), Times.Once);
//         blockHeight.ShouldBe(200);
//     }
//     
//     
//     [Fact]
//     public async Task GetChainIdsAsync_ShouldReturnListOfChainIds()
//     {
//         // Arrange
//         var chainIds = new List<string> { ChainIdTDVV, ChainIdAELF };
//         
//         _chainAppServiceMock.Setup(x => x.GetListAsync()).ReturnsAsync(chainIds.ToArray);
//
//         // Act
//         var result = await _proposalUpdateService.GetChainIdsAsync();
//
//         // Assert
//         result.ShouldContain(ChainIdTDVV);
//         result.ShouldContain(ChainIdAELF);
//     }
// }