using System;
using System.Collections.Generic;
using System.Threading;
using AElf;
using GraphQL;
using GraphQL.Client.Abstractions;
using Moq;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp;
using DateTime = System.DateTime;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Common.Mocks;

public class GraphQLClientMock
{
    public static IGraphQLClient MockGraphQLClient<TT>(Func<GraphQLRequest, TT> func)
    {
        var mock = new Mock<IGraphQLClient>();
        mock.Setup(m => m.SendQueryAsync<TT>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TT>
            {
                Data = func(request)
            });
        return mock.Object;
    }

    public static IGraphQLClient MockGraphQLClient<TT, TK>(Func<GraphQLRequest, TT> func,
        Func<GraphQLRequest, TK>? funcTk)
    {
        var mock = new Mock<IGraphQLClient>();
        mock.Setup(m => m.SendQueryAsync<TT>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TT>
            {
                Data = func(request)
            });
        mock.Setup(m => m.SendQueryAsync<TK>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TK>
            {
                Data = funcTk(request)
            });

        return mock.Object;
    }

    public static IGraphQLClient MockGraphQLClient<TT, TK, TV>(Func<GraphQLRequest, TT> func,
        Func<GraphQLRequest, TK>? funcTk, Func<GraphQLRequest, TV>? funcTv)
    {
        var mock = new Mock<IGraphQLClient>();
        mock.Setup(m => m.SendQueryAsync<TT>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TT>
            {
                Data = func(request)
            });
        mock.Setup(m => m.SendQueryAsync<TK>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TK>
            {
                Data = funcTk(request)
            });
        mock.Setup(m => m.SendQueryAsync<TV>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TV>
            {
                Data = funcTv(request)
            });
        return mock.Object;
    }

    public static IGraphQLClient MockGraphQLClient<TT, TK, TV, TI>(Func<GraphQLRequest, TT> func,
        Func<GraphQLRequest, TK>? funcTk, Func<GraphQLRequest, TV>? funcTv, Func<GraphQLRequest, TI>? funcTi)
    {
        var mock = new Mock<IGraphQLClient>();
        mock.Setup(m => m.SendQueryAsync<TT>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TT>
            {
                Data = func(request)
            });
        mock.Setup(m => m.SendQueryAsync<TK>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TK>
            {
                Data = funcTk(request)
            });
        mock.Setup(m => m.SendQueryAsync<TV>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TV>
            {
                Data = funcTv(request)
            });
        mock.Setup(m => m.SendQueryAsync<TI>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TI>
            {
                Data = funcTi(request)
            });
        return mock.Object;
    }

    public static IGraphQLClient MockGraphQLClient<T>(T results)
    {
        T Func(GraphQLRequest request) => results;
        return MockGraphQLClient(Func);
    }

    public static IGraphQLClient MockGraphQLClient<T, K>(T resultsT, K resultsK)
    {
        T FuncT(GraphQLRequest request) => resultsT;
        K FuncK(GraphQLRequest request) => resultsK;
        return MockGraphQLClient(FuncT, FuncK);
    }

    public static IGraphQLClient MockElectionCandidateElectedDto()
    {
        return MockGraphQLClient(
            new IndexerCommonResult<ElectionPageResultDto<ElectionCandidateElectedDto>>()
            {
                Data = new ElectionPageResultDto<ElectionCandidateElectedDto>
                {
                    Items = new[]
                    {
                        new ElectionCandidateElectedDto
                        {
                            Id = "11",
                            DaoId = HashHelper.ComputeFrom("DaoId1").ToHex(),
                            PreTermNumber = 1,
                            NewNumber = 2,
                            CandidateElectedTime = DateTime.Now,
                            ChainId = ChainIdAELF,
                            BlockHash = HashHelper.ComputeFrom("BlockHash").ToHex(),
                            BlockHeight = 1000,
                            PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash").ToHex(),
                            IsDeleted = false
                        }
                    },
                    TotalCount = 1
                }
            });
    }

    public static IGraphQLClient MockElectionHighCouncilConfigDto()
    {
        return MockGraphQLClient(new IndexerCommonResult<ElectionPageResultDto<ElectionHighCouncilConfigDto>>()
        {
            Data = new ElectionPageResultDto<ElectionHighCouncilConfigDto>
            {
                Items = new[]
                {
                    new ElectionHighCouncilConfigDto
                    {
                        Id = "11",
                        DaoId = HashHelper.ComputeFrom("DaoId2").ToHex(),
                        ChainId = ChainIdAELF,
                        BlockHash = HashHelper.ComputeFrom("BlockHash").ToHex(),
                        BlockHeight = 1000,
                        PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash").ToHex(),
                        IsDeleted = false
                    }
                },
                TotalCount = 1
            }
        });
    }

    public static IGraphQLClient MockElectionVotingItemDto()
    {
        var func = (GraphQLRequest request) =>
        {
            if (request.Variables == null || request.Variables.ToString().IndexOf("ThrowException") != -1)
            {
                throw new UserFriendlyException("GraphQL query exception.");
            }

            return new IndexerCommonResult<ElectionPageResultDto<ElectionVotingItemDto>>
            {
                Data = new ElectionPageResultDto<ElectionVotingItemDto>
                {
                    Items = new[]
                    {
                        new ElectionVotingItemDto
                        {
                            Id = "Id",
                            DaoId = "DaoId",
                            VotingItemId = "VotingItemId",
                            AcceptedCurrency = "ELF",
                            IsLockToken = false,
                            CurrentSnapshotNumber = 1,
                            TotalSnapshotNumber = 1,
                            Options = null,
                            RegisterTimestamp = DateTime.Now,
                            StartTimestamp = DateTime.Now,
                            EndTimestamp = DateTime.Now,
                            CurrentSnapshotStartTimestamp = DateTime.Now,
                            Sponsor = "Sponsor",
                            IsQuadratic = false,
                            TicketCost = 0,
                            ChainId = ChainIdAELF,
                            BlockHash = "BlockHash",
                            BlockHeight = 100,
                            PreviousBlockHash = "PreviousBlockHash",
                            IsDeleted = false
                        }
                    },
                    TotalCount = 10
                }
            };
        };
        return MockGraphQLClient(func);
    }

    public static IGraphQLClient MockTreasuryProviderGraphQL()
    {
        IndexerCommonResult<GetTreasuryFundListResult> FuncTreasuryFundList(GraphQLRequest request)
        {
            if (request.Variables != null && request.Variables.ToString().IndexOf("ThrowException") != -1)
            {
                throw new UserFriendlyException("GraphQL query exception.");
            }

            return new IndexerCommonResult<GetTreasuryFundListResult>
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
            };
        }

        IndexerCommonResult<GetTreasuryRecordListResult> FuncTreasuryRecordList(GraphQLRequest request)
        {
            if (request.Variables != null && request.Variables.ToString().IndexOf("ThrowException") != -1)
            {
                throw new UserFriendlyException("GraphQL query exception.");
            }

            return new IndexerCommonResult<GetTreasuryRecordListResult>
            {
                Data = new GetTreasuryRecordListResult
                {
                    Item1 = 10,
                    Item2 = new List<TreasuryRecordDto>()
                    {
                        new TreasuryRecordDto
                        {
                            Id = "Id",
                            ChainId = ChainIdAELF,
                            BlockHeight = 100,
                            DaoId = "DaoId",
                            TreasuryAddress = "TreasuryAddress",
                            Amount = 100000,
                            Symbol = "ELF",
                            Executor = "Executor",
                            FromAddress = "FromAddress",
                            ToAddress = "ToAddress",
                            Memo = "Memo",
                            TreasuryRecordType = 3,
                            CreateTime = DateTime.Now,
                            ProposalId = "ProposalId"
                        }
                    }
                }
            };
        }

        return MockGraphQLClient(FuncTreasuryFundList, FuncTreasuryRecordList);
    }
}