// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using AutoMapper;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Moq;
// using Nest;
// using Shouldly;
// using TomorrowDAOServer.Chains;
// using TomorrowDAOServer.Common.Provider;
// using TomorrowDAOServer.Entities;
// using TomorrowDAOServer.Enums;
// using TomorrowDAOServer.Options;
// using TomorrowDAOServer.Organization.Index;
// using TomorrowDAOServer.Organization.Provider;
// using TomorrowDAOServer.Proposal.Index;
// using TomorrowDAOServer.Proposal.Provider;
// using TomorrowDAOServer.Vote.Index;
// using TomorrowDAOServer.Vote.Provider;
// using Volo.Abp.Caching;
// using Volo.Abp.ObjectMapping;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace TomorrowDAOServer.Proposal;
//
// public class ProposalSyncDataServiceTest : TomorrowDAOServerApplicationTestBase
// {
//     private ProposalSyncDataService _proposalSyncDataService;
//     private Mock<ILogger<ProposalSyncDataService>> _loggerMock;
//     private Mock<IProposalProvider> _proposalProviderMock;
//     private Mock<IChainAppService> _chainAppServiceMock;
//     private Mock<IDistributedCache<List<string>>> _distributedCacheMock;
//     private Mock<IVoteProvider> _voteProviderMock;
//     private Mock<IOrganizationInfoProvider> _organizationInfoProviderMock;
//     private Mock<IProposalAssistService> _proposalAssistServiceMock;
//     private IObjectMapper _objectMapper;
//     private IOptionsMonitor<SyncDataOptions> _syncDataOptionsMonitor;
//
//     public ProposalSyncDataServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
//     {
//         _objectMapper = GetRequiredService<IObjectMapper>();
//         _syncDataOptionsMonitor = GetRequiredService<IOptionsMonitor<SyncDataOptions>>();
//         _loggerMock = new Mock<ILogger<ProposalSyncDataService>>();
//         _proposalProviderMock = MockProposalProvider();
//         _chainAppServiceMock = new Mock<IChainAppService>();
//         _distributedCacheMock = new Mock<IDistributedCache<List<string>>>();
//         _voteProviderMock = new Mock<IVoteProvider>();
//         _organizationInfoProviderMock = new Mock<IOrganizationInfoProvider>();
//         _proposalAssistServiceMock = new Mock<IProposalAssistService>();
//         _proposalSyncDataService = new ProposalSyncDataService(
//             _loggerMock.Object,
//             It.IsAny<IGraphQLProvider>(),
//             _proposalProviderMock.Object,
//             _chainAppServiceMock.Object,
//             _distributedCacheMock.Object,
//             _syncDataOptionsMonitor,
//             _objectMapper,
//             _voteProviderMock.Object,
//             _organizationInfoProviderMock.Object,
//             _proposalAssistServiceMock.Object);
//     }
//
//     private List<IndexerProposal> MockIndexerProposals()
//     {
//         return new List<IndexerProposal>
//         {
//             new IndexerProposal { ProposalId1 = "1", ProposalStatus = ProposalStatus.Active, BlockHeight = 100, 
//                 OrganizationAddress = "address1", ExpiredTime = DateTime.UtcNow.AddDays(-1)},
//             new IndexerProposal { ProposalId1 = "2", ProposalStatus = ProposalStatus.Active, BlockHeight = 200, VoteFinished = true,
//                 OrganizationAddress = "address2", ExpiredTime = DateTime.UtcNow.AddDays(1)}
//         };
//     }
//     
//     private List<ProposalIndex> MockProposals()
//     {
//         return new List<ProposalIndex>
//         {
//             new ProposalIndex
//             {
//                 ProposalId1 = "1", ProposalStatus = ProposalStatus.Approved, BlockHeight = 99, ExpiredTime = DateTime.UtcNow.AddDays(-1)
//             },
//             new ProposalIndex
//             {
//                 ProposalId1 = "2", ProposalStatus = ProposalStatus.Approved, BlockHeight = 199, ExpiredTime = DateTime.UtcNow.AddDays(1)
//             }
//         };
//     }
//
//     private Mock<IProposalProvider> MockProposalProvider()
//     {
//         var mock = new Mock<IProposalProvider>();
//         var indexerProposals = MockIndexerProposals();
//
//         var proposalIndexList = MockProposals();
//         
//         mock.Setup(x =>
//                 x.GetSyncProposalDataAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long>(),
//                     It.IsAny<long>()))
//             .ReturnsAsync(indexerProposals);
//         
//         mock.Setup(x =>
//                 x.GetExpiredProposalListAsync(It.Is<int>(p => p == 0), It.IsAny<List<ProposalStatus>>()))
//             .ReturnsAsync(proposalIndexList);
//         
//         //for break for-each
//         mock.Setup(x =>
//                 x.GetSyncProposalDataAsync(It.Is<int>(p => p > 0), It.IsAny<string>(), It.IsAny<long>(),
//                     It.IsAny<long>()))
//             .ReturnsAsync(new List<IndexerProposal>());
//         
//         
//         mock.Setup(x =>
//                 x.GetProposalListByIds(It.IsAny<string>(), It.IsAny<List<string>>()))
//             .ReturnsAsync(proposalIndexList.Select(item => item).ToDictionary(item => item.ProposalId1, item => item));
//
//         mock.Setup(x => x.BulkAddOrUpdateAsync(It.IsAny<List<ProposalIndex>>()))
//             .Returns(Task.CompletedTask);
//         
//         return mock;
//
//     }
//
//
//     [Fact]
//     public async Task SyncIndexerRecordsAsync_ShouldSyncProposalData_AndUpdateCache()
//     {
//         // Arrange
//         var cachedProposalIds = new List<string> { "1" };
//         
//         _distributedCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
//             .ReturnsAsync(cachedProposalIds);
//         
//         _voteProviderMock.Setup(x => x.GetVoteInfosMemoryAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
//             .ReturnsAsync(new Dictionary<string, IndexerVote>
//             {
//                 ["1"] = new IndexerVote { ApprovedCount = 10 },
//                 ["2"] = new IndexerVote { ApprovedCount = 10 }
//             });
//
//         _organizationInfoProviderMock
//             .Setup(x => x.GetOrganizationInfosMemoryAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
//             .ReturnsAsync(new Dictionary<string, IndexerOrganizationInfo>
//             {
//                 ["address1"] = new IndexerOrganizationInfo { OrganizationMemberCount = 10 },
//                 ["address2"] = new IndexerOrganizationInfo { OrganizationMemberCount = 10 }
//             });
//
//         _proposalAssistServiceMock.Setup(x =>
//                 x.ToProposalResult(It.IsAny<ProposalIndex>(), It.IsAny<IndexerVote>(),
//                     It.IsAny<IndexerOrganizationInfo>()))
//             .Returns(ProposalStatus.Rejected);
//
//         // Act
//         var blockHeight = await _proposalSyncDataService.SyncIndexerRecordsAsync(ChainIdTDVV, 0, 1000);
//
//         // Assert
//         blockHeight.ShouldBe(200);
//         _proposalProviderMock.Verify(x => x.BulkAddOrUpdateAsync(It.IsAny<List<ProposalIndex>>()), Times.Once);
//         _distributedCacheMock.Verify(
//             expression: x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<DistributedCacheEntryOptions>(), 
//                 null, It.IsAny<bool>(), It.IsAny<CancellationToken>()),
//             Times.Once);
//     }
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
//         var result = await _proposalSyncDataService.GetChainIdsAsync();
//
//         // Assert
//         result.ShouldContain(ChainIdTDVV);
//         result.ShouldContain(ChainIdAELF);
//     }
// }