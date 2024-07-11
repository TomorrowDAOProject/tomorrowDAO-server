// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Linq.Expressions;
// using System.Threading.Tasks;
// using AElf.Indexing.Elasticsearch;
// using GraphQL;
// using Microsoft.Extensions.DependencyInjection;
// using Moq;
// using Nest;
// using Newtonsoft.Json;
// using Shouldly;
// using TomorrowDAOServer.Common.GraphQL;
// using TomorrowDAOServer.Entities;
// using TomorrowDAOServer.Enums;
// using TomorrowDAOServer.Proposal.Dto;
// using TomorrowDAOServer.Proposal.Index;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace TomorrowDAOServer.Proposal.Provider;
//
// public sealed class ProposalProviderTest : TomorrowDAOServerApplicationTestBase
// {
//     private readonly IProposalProvider _proposalProvider;
//
//     public ProposalProviderTest(ITestOutputHelper output) : base(output)
//     {
//         _proposalProvider = GetRequiredService<ProposalProvider>();
//     }
//
//     protected override void AfterAddApplication(IServiceCollection services)
//     {
//         services.AddSingleton(MockProposalRepository());
//         services.AddSingleton(MockIGraphQlHelper());
//     }
//
//     private IGraphQlHelper MockIGraphQlHelper()
//     {
//         var mock = new Mock<IGraphQlHelper>();
//         
//         mock
//             .Setup(m => m.QueryAsync<IndexerProposalSync>(It.IsAny<GraphQLRequest>()))
//             .ReturnsAsync(MockGetSyncProposalInfos);
//         //TODO
//         return mock.Object;
//     }
//     
//     private static IndexerProposalSync MockGetSyncProposalInfos()
//     {
//         return new IndexerProposalSync
//         {
//             TotalRecordCount = 10,
//             DataList = JsonConvert.DeserializeObject<List<IndexerProposal>>(ProposalListJson)
//         };
//     }
//
//     private INESTRepository<ProposalIndex, string> MockProposalRepository()
//     {
//         var mock = new Mock<INESTRepository<ProposalIndex, string>>();
//
//         // Mock the repository method
//         mock.Setup(r => r.GetSortListAsync(
//                 It.IsAny<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
//                 It.IsAny<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
//                 It.IsAny<Func<SortDescriptor<ProposalIndex>, IPromise<IList<ISort>>>>(),
//                 It.IsAny<int>(),
//                 It.IsAny<int>(),
//                 It.IsAny<string>()))
//             .ReturnsAsync(MockProposalList());
//         
//         mock.Setup(r => r.GetAsync(
//                 It.IsAny<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
//                 It.IsAny<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
//                 It.IsAny<Expression<Func<ProposalIndex, object>>>(),
//                 It.IsAny<SortOrder>(),
//                 It.IsAny<string>()))!
//             .ReturnsAsync(MockProposalList().Item2.Where(item => item.ChainId.Equals(ChainIdTDVV) && item.ProposalId1.Equals(ProposalId1))
//                 .Select(item => item).FirstOrDefault);
//
//         mock.Setup(r => r.GetListAsync(
//                 It.IsAny<Func<QueryContainerDescriptor<ProposalIndex>, QueryContainer>>(),
//                 It.IsAny<Func<SourceFilterDescriptor<ProposalIndex>, ISourceFilter>>(),
//                 It.IsAny<Expression<Func<ProposalIndex, object>>>(),
//                 It.IsAny<SortOrder>(),
//                 It.IsAny<int>(),
//                 It.IsAny<int>(),
//                 It.IsAny<string>()))
//             .ReturnsAsync(MockProposalList());
//         
//         mock.Setup(r => r.BulkAddOrUpdateAsync(
//                 It.IsAny<List<ProposalIndex>>(),
//                 It.IsAny<string>()))
//             .Returns(Task.CompletedTask);
//         
//         return mock.Object;
//     }
//     
//     [Fact]
//     public async void GetSyncProposalDataAsync_Test()
//     {
//         // Arrange
//         var skipCount = 0;
//         var chainId = ChainIdTDVV;
//         var startBlockHeight = 0;
//         var endBlockHeight = 9999;
//         
//         // Act
//         var result = await _proposalProvider.GetSyncProposalDataAsync(skipCount, chainId, startBlockHeight, endBlockHeight);
//
//         // Assert
//         result.ShouldNotBeNull();
//         result.Count.ShouldBe(2);
//     }
//     
//     [Fact]
//     public async void GetProposalListAsync_Test()
//     {
//         // Arrange
//         var input = new QueryProposalListInput()
//         {
//             ChainId = ChainIdTDVV,
//             DaoId = DAOId,
//             GovernanceMechanism = GovernanceMechanism.Parliament,
//             ProposalType = ProposalType.Governance,
//             ProposalStatus = ProposalStatus.Active,
//             Content = ProposalId1
//         };
//         // Act
//         var result = await _proposalProvider.GetProposalListAsync(input);
//
//         // Assert
//         result.ShouldNotBeNull();
//         result.Item1.ShouldBe(10);
//         result.Item2.ShouldNotBeNull();
//         result.Item2.Count.ShouldBe(2);
//     }
//
//     [Fact]
//     public async void GetProposalByIdAsync_Test()
//     {
//         // Arrange
//         var chainId = ChainIdTDVV;
//         var proposalId = ProposalId1;
//         // Act
//         var result = await _proposalProvider.GetProposalByIdAsync(chainId, proposalId);
//
//         // Assert
//         result.ShouldNotBeNull();
//         result.ProposalId1.ShouldBe(proposalId);
//     }
//     
//     [Fact]
//     public async void GetProposalListByIds_Test()
//     {
//         // Arrange
//         var chainId = ChainIdTDVV;
//         var proposalId = new List<string>
//         {
//             ProposalId1
//         };
//         // Act
//         var result = await _proposalProvider.GetProposalListByIds(chainId, proposalId);
//
//         // Assert
//         result.ShouldNotBeNull();
//         result.ShouldContainKey(ProposalId1);
//      }
//     
//     [Fact]
//     public async void BulkAddOrUpdateAsync_Test()
//     {
//         // Arrange
//         var proposalIndices = MockProposalList().Item2;
//         // Act
//          await _proposalProvider.BulkAddOrUpdateAsync(proposalIndices);
//     }
//     
//     [Fact]
//     public async void GetExpiredProposalListAsync_Test()
//     {
//         // Arrange
//         var statusList = new List<ProposalStatus>
//         {
//             ProposalStatus.Approved
//         };
//         
//         // Act
//         var result = await _proposalProvider.GetExpiredProposalListAsync(0, statusList);
//         
//         // Assert
//         result.ShouldNotBeNull();
//     }
// }