using System.Collections.Generic;
using AElf;
using GraphQL;
using Moq;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.NetworkDao.Dto;
using TomorrowDAOServer.Providers;

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
    
    private void MockExplorerRequest()
    {
        HttpRequestMock.MockHttpByPath(ExplorerApi.ProposalList.Method, ExplorerApi.ProposalList.Path,
            new ExplorerBaseResponse<ExplorerProposalResponse>
            {
                Code = 0,
                Msg = null,
                Data = new ExplorerProposalResponse
                {
                    Total = 1,
                    List = new List<ExplorerProposalResult>()
                    {
                        new ExplorerProposalResult
                        {
                            Abstentions = null,
                            Approvals = null,
                            CanVote = false,
                            ContractAddress = null,
                            ContractMethod = null,
                            CreateAt = default,
                            CreateTxId = null,
                            CreatedBy = null,
                            ExpiredTime = default,
                            Id = 0,
                            IsContractDeployed = false,
                            LeftInfo = null,
                            OrganizationAddress = null,
                            OrgAddress = null,
                            OrganizationInfo = null,
                            ProposalType = null,
                            TxId = null,
                            UpdatedAt = default,
                            ProposalId = ProposalId1,
                            Proposer = null,
                            Rejections = 0,
                            ReleasedTime = default,
                            ReleasedTxId = null,
                            Status = null,
                            VotedStatus = null,
                            Title = null,
                            Description = null
                        },
                        new ExplorerProposalResult
                        {
                            Abstentions = null,
                            Approvals = null,
                            CanVote = false,
                            ContractAddress = null,
                            ContractMethod = null,
                            CreateAt = default,
                            CreateTxId = null,
                            CreatedBy = null,
                            ExpiredTime = default,
                            Id = 0,
                            IsContractDeployed = false,
                            LeftInfo = null,
                            OrganizationAddress = null,
                            OrgAddress = null,
                            OrganizationInfo = null,
                            ProposalType = null,
                            TxId = null,
                            UpdatedAt = default,
                            ProposalId = ProposalId2,
                            Proposer = null,
                            Rejections = 0,
                            ReleasedTime = default,
                            ReleasedTxId = null,
                            Status = null,
                            VotedStatus = null,
                            Title = null,
                            Description = null
                        }
                    },
                    BpCount = 17
                }
            });
    }
}