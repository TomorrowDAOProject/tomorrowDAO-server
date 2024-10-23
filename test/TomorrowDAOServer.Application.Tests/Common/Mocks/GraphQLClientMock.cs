using System;
using System.Collections.Generic;
using System.Threading;
using AElf;
using GraphQL;
using GraphQL.Client.Abstractions;
using Moq;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp;
using DateTime = System.DateTime;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Common.Mocks;

public class GraphQLClientMock
{
    public static IGraphQLClient MockGraphQLClient()
    {
        var mock = new Mock<IGraphQLClient>();

        //mock GraphQL(Mock[GraphQLMethodName])
        MockElectionCandidateElectedDto(mock);
        MockElectionHighCouncilConfigDto(mock);
        MockElectionVotingItemDto(mock);
        MockGetTreasuryFundListResultAndGetTreasuryRecordListResult(mock);
        MockPageResultDto_MemberDto(mock);
        MockGetDAOList(mock);
        MockGetMyParticipated(mock);
        MockGetMember(mock);
        
        

        return mock.Object;
    }

    public static void MockGraphQLClient<T>(Mock<IGraphQLClient> mock, T results)
    {
        MockGraphQLClient(mock, (GraphQLRequest request) => request);
    }

    public static void MockGraphQLClient<T, K>(Mock<IGraphQLClient> mock, T resultsT, K resultsK)
    {
        T FuncT(GraphQLRequest request) => resultsT;
        K FuncK(GraphQLRequest request) => resultsK;
        MockGraphQLClient(mock, FuncT, FuncK);
    }

    public static void MockGraphQLClient<TT>(Mock<IGraphQLClient> mock, Func<GraphQLRequest, TT> func)
    {
        mock.Setup(m => m.SendQueryAsync<TT>(It.IsAny<GraphQLRequest>(), default)).ReturnsAsync(
            (GraphQLRequest request, CancellationToken cancellationToken) => new GraphQLResponse<TT>
            {
                Data = func(request)
            });
    }

    public static void MockGraphQLClient<TT, TK>(Mock<IGraphQLClient> mock, Func<GraphQLRequest, TT> func,
        Func<GraphQLRequest, TK>? funcTk)
    {
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
    }

    public static void MockGraphQLClient<TT, TK, TV>(Mock<IGraphQLClient> mock, Func<GraphQLRequest, TT> func,
        Func<GraphQLRequest, TK>? funcTk, Func<GraphQLRequest, TV>? funcTv)
    {
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
    }

    public static void MockGraphQLClient<TT, TK, TV, TI>(Mock<IGraphQLClient> mock, Func<GraphQLRequest, TT> func,
        Func<GraphQLRequest, TK>? funcTk, Func<GraphQLRequest, TV>? funcTv, Func<GraphQLRequest, TI>? funcTi)
    {
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
    }

    public static void MockElectionCandidateElectedDto(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock,
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

    public static void MockElectionHighCouncilConfigDto(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock, new IndexerCommonResult<ElectionPageResultDto<ElectionHighCouncilConfigDto>>()
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

    public static void MockElectionVotingItemDto(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock, (GraphQLRequest request) =>
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
        });
    }

    public static void MockGetTreasuryFundListResultAndGetTreasuryRecordListResult(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock, (GraphQLRequest request) =>
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
        }, (GraphQLRequest request) =>
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
        });
    }
    
    private static void MockPageResultDto_MemberDto(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock, new IndexerCommonResult<PageResultDto<MemberDto>>());
    }
    
    private static void MockGetDAOList(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock, (GraphQLRequest request) =>
        {
            if (request.Variables != null && request.Variables.ToString().IndexOf("ThrowException") != -1)
            {
                throw new UserFriendlyException("GraphQL query exception.");
            }

            var skipCount = (int) GetVariableValue(request.Variables, "skipCount");
            if (skipCount > 0)
            {
                return new IndexerDAOInfos();
            }

            return new IndexerDAOInfos
            {
                DAOInfos = new List<IndexerDAOInfo>() { new IndexerDAOInfo
                    {
                        Id = DaoId,
                        ChainId = ChainIdAELF,
                        BlockHeight = 1000,
                        Creator = Address1,
                        Metadata = new IndexerMetadata(),
                        GovernanceToken = ELF,
                        IsHighCouncilEnabled = false,
                        HighCouncilAddress = null,
                        MaxHighCouncilMemberCount = 0,
                        MaxHighCouncilCandidateCount = 0,
                        ElectionPeriod = 0,
                        StakingAmount = 0,
                        HighCouncilTermNumber = 0,
                        FileInfoList = null,
                        IsTreasuryContractNeeded = false,
                        SubsistStatus = false,
                        TreasuryContractAddress = null,
                        TreasuryAccountAddress = null,
                        IsTreasuryPause = false,
                        TreasuryPauseExecutor = null,
                        VoteContractAddress = null,
                        ElectionContractAddress = null,
                        GovernanceContractAddress = null,
                        TimelockContractAddress = null,
                        PermissionAddress = null,
                        PermissionInfoList = null,
                        CreateTime = default,
                        IsNetworkDAO = false,
                        VoterCount = 0,
                        GovernanceMechanism = GovernanceMechanism.Referendum
                    }
                }
            };
        });
    }
    
    private static void MockGetMyParticipated(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock, (GraphQLRequest request) =>
        {
            if (request.Variables != null && request.Variables.ToString().IndexOf("ThrowException") != -1)
            {
                throw new UserFriendlyException("GraphQL query exception.");
            }

            return new IndexerCommonResult<PageResultDto<IndexerDAOInfo>>
            {
                Data = new PageResultDto<IndexerDAOInfo>
                {
                    TotalCount = 1,
                    Data = new List<IndexerDAOInfo>()
                    {
                        new IndexerDAOInfo
                        {
                            Id = DaoId,
                            ChainId = ChainIdAELF,
                            BlockHeight = 1000,
                            Creator = Address1,
                            Metadata = new IndexerMetadata(),
                            GovernanceToken = ELF,
                            IsHighCouncilEnabled = false,
                            HighCouncilAddress = null,
                            MaxHighCouncilMemberCount = 0,
                            MaxHighCouncilCandidateCount = 0,
                            ElectionPeriod = 0,
                            StakingAmount = 0,
                            HighCouncilTermNumber = 0,
                            FileInfoList = null,
                            IsTreasuryContractNeeded = false,
                            SubsistStatus = false,
                            TreasuryContractAddress = null,
                            TreasuryAccountAddress = null,
                            IsTreasuryPause = false,
                            TreasuryPauseExecutor = null,
                            VoteContractAddress = null,
                            ElectionContractAddress = null,
                            GovernanceContractAddress = null,
                            TimelockContractAddress = null,
                            PermissionAddress = null,
                            PermissionInfoList = null,
                            CreateTime = default,
                            IsNetworkDAO = false,
                            VoterCount = 0,
                            GovernanceMechanism = GovernanceMechanism.Referendum
                        }
                    }
                }
            };
        });
    }

    private static void MockGetMember(Mock<IGraphQLClient> mock)
    {
        MockGraphQLClient(mock, (GraphQLRequest request) =>
        {
            if (request.Variables != null && request.Variables.ToString().IndexOf("ThrowException") != -1)
            {
                throw new UserFriendlyException("GraphQL query exception.");
            }

            return new IndexerCommonResult<MemberDto>
            {
                Data = new MemberDto
                {
                    Id = "Id",
                    ChainId = ChainIdAELF,
                    BlockHeight = 1000,
                    DAOId = DaoId,
                    Address = Address1,
                    CreateTime = DateTime.Now
                }
            };
        });
    }

    private static object GetVariableValue(object variablesObj, string variableName)
    {
        var propertyInfo = variablesObj.GetType().GetProperty(variableName);
        return propertyInfo?.GetValue(variablesObj);
    }

    protected static string GraphQlMethodPattern(string methodName)
    {
        return @"(?<![.\w])" + methodName + @"\s*(?=\()";
    }
}