using AElf;
using GraphQL;
using Moq;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.NetworkDao.Dto;

namespace TomorrowDAOServer.NetworkDao;

public partial class NetworkDaoTest
{
    private static IGraphQlHelper MockGraphQlHelper_NetworkDaoProposalDto()
    {
        var mock = new Mock<IGraphQlHelper>();
        mock.Setup(m =>
                m.QueryAsync<IndexerCommonResult<NetworkDaoPagedResultDto<NetworkDaoProposalDto>>>(
                    It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new IndexerCommonResult<NetworkDaoPagedResultDto<NetworkDaoProposalDto>>()
            {
                Data = new NetworkDaoPagedResultDto<NetworkDaoProposalDto>
                {
                    Items = new[]
                    {
                        new NetworkDaoProposalDto
                        {
                            ProposalId = ProposalId1,
                            OrganizationAddress = Address1,
                            Title = "ProposalId1 Title",
                            Description = "ProposalId1 Description",
                            ProposalType = (int)NetworkDaoProposalType.Referendum,
                            Id = "11",
                            ChainId = ChainIdAELF,
                            BlockHash = HashHelper.ComputeFrom("BlockHash")
                                .ToHex(),
                            BlockHeight = 1000,
                            PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash")
                                .ToHex(),
                            IsDeleted = false
                        }
                    },
                    TotalCount = 1
                }
            });
        return mock.Object;
    }
}