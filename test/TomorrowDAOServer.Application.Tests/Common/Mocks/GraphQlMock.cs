using System.Collections.Generic;
using GraphQL;
using Moq;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Treasury.Dto;

namespace TomorrowDAOServer.Common.Mocks;

public class GraphQlMock
{
    public static IGraphQlHelper MockGraphQlHelper<T>(T returns)
    {
        var mock = new Mock<IGraphQlHelper>();
        mock.Setup(m => m.QueryAsync<T>(
            It.IsAny<GraphQLRequest>())).ReturnsAsync(returns);
        return mock.Object;
    }

    public static IGraphQlHelper MockGetTreasuryFundListResult()
    {
        return MockGraphQlHelper<IndexerCommonResult<GetTreasuryFundListResult>>(
            new IndexerCommonResult<GetTreasuryFundListResult>
            {
                Data = new GetTreasuryFundListResult
                {
                    Item1 = 10,
                    Item2 = new List<TreasuryFundDto>()
                    {
                        new TreasuryFundDto
                        {
                            Id = "Id",
                            ChainId = "AELF",
                            BlockHeight = 100,
                            DaoId = "DaoId",
                            TreasuryAddress = "TreasuryAddress",
                            Symbol = "ELF",
                            AvailableFunds = 100000000,
                            LockedFunds = 0
                        }
                    }
                }
            });
    }
}