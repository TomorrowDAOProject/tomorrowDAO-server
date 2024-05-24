using System;
using System.Collections.Generic;
using GraphQL;
using Moq;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Governance.Dto;

namespace TomorrowDAOServer.Governance;

public partial class GovernanceServiceTest
{
    private static IGraphQlHelper MockGraphQlHelper_QueryIndexerGovernanceSchemeDto()
    {
        var mock = new Mock<IGraphQlHelper>();
        mock.Setup(m => m.QueryAsync<IndexerGovernanceSchemeDto>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new IndexerGovernanceSchemeDto
            {
                Data = new List<IndexerGovernanceScheme>
                {
                    {
                        new IndexerGovernanceScheme
                        {
                            Id = "id",
                            DAOId = "daoId",
                            SchemeId = "schemeId",
                            SchemeAddress = "SchemeAddress",
                            ChainId = "ChainId",
                            GovernanceMechanism = GovernanceMechanism.Referendum,
                            GovernanceToken = "ELF",
                            CreateTime = DateTime.UtcNow,
                            MinimalRequiredThreshold = 1,
                            MinimalVoteThreshold = 2,
                            MinimalApproveThreshold = 3,
                            MaximalRejectionThreshold = 4,
                            MaximalAbstentionThreshold = 5
                        }
                    },
                    {
                        new IndexerGovernanceScheme()
                    }
                }
            });
        return mock.Object;
    }
}