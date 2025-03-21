using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NSubstitute;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Users;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.EntityEventHandler.Tests;

public abstract class TomorrowDAOServerEntityEventHandlerTestsBase : TomorrowDAOServerTestBase<
    TomorrowDAOServerEntityEventHandlerTestsModule>
{
    protected const string ChainIdTDVV = "tDVV";
    protected const string ChainIdAELF = "AELF";
    protected const string ELF = "ELF";
    protected const string ProposalId1 = "99df86594a989227b8e6259f70b08976812537c20486717a3d0158788155b1f0";
    protected const string ProposalId2 = "40510452a04b0857003be9bc222e672c7aff3bf3d4d858a5d72ad2df409b7b6d";
    protected const string ProposalId3 = "bf0cc1d7f7adcc2a43a6cc08cc303719aad51196da7570ebd62eca8ed1100cf6";
    protected const string DAOId = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";
    protected const string DAOName = "DAOName";
    protected static readonly string PrivateKey1 = Environment.GetEnvironmentVariable("UNITTEST_KEY_01");

    protected const string PublicKey1 =
        "04f5db833e5377cab193e3fc663209ac3293ef67736021ee9cebfd1b95a058a5bb400aaeb02ed15dc93177c9bcf38057c4b8069f46601a2180e892a555345c89cf";

    protected const string Address1 = "2Md6Vo6SWrJPRJKjGeiJtrJFVkbc5EARXHGcxJoeD75pMSfdN2";
    protected const string Address1CaHash = "c4e3d170923689c63f827add21a0312b553f9d18de02a77282c5e9fee411daf1";

    protected static readonly string PrivateKey2 = Environment.GetEnvironmentVariable("UNITTEST_KEY_02");

    protected const string PublicKey2 =
        "04de4367b534d76e8586ac191e611c4ac05064b8bc585449aee19a8818e226ad29c24559216fd33c28abe7acaa8471d2b521152e8b40290dfc420d6eb89f70861a";

    protected const string Address2 = "2DA5orGjmRPJBCDiZQ76NSVrYm7Sn5hwgVui76kCJBMFJYxQFw";

    protected const string Address3 = "2gehnpYhWTSsWLphakYLBU343LJ6c4BjxQf1yDDdjuhaC3G2x7";

    protected readonly Mock<IUserProvider> UserProviderMock = new();
    protected readonly IUserProvider UserProvider = new Mock<IUserProvider>().Object;
    protected readonly ICurrentUser CurrentUser = Substitute.For<ICurrentUser>();

    private readonly IReferralCycleProvider _referralCycleProvider;

    public TomorrowDAOServerEntityEventHandlerTestsBase(ITestOutputHelper output) : base(output)
    {
        _referralCycleProvider = Application.ServiceProvider.GetRequiredService<IReferralCycleProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAbpDistributedLockAlwaysSuccess());
        services.AddSingleton(MockGraphQlOptions());
        services.AddSingleton(MockExplorerOptions());
        services.AddSingleton(MockQueryContractOption());
        services.AddSingleton(MockContractInfoOptions());
        services.AddSingleton(MockChainOptions());
        services.AddSingleton(MockApiOption());
        services.AddSingleton(MockRankingOptions());
        services.AddSingleton(MockUserOptions());
        services.AddSingleton(MockTelegramOptions());
        services.AddSingleton(HttpRequestMock.MockHttpFactory());
        services.AddSingleton(ContractProviderMock.MockContractProvider());
        services.AddSingleton(GraphQLClientMock.MockGraphQLClient());
        services.AddSingleton(UserProviderMock.Object);
        services.AddSingleton(CurrentUser);
    }

    private IOptionsMonitor<TelegramOptions> MockTelegramOptions()
    {
        var mock = new Mock<IOptionsMonitor<TelegramOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new TelegramOptions
        {
            AllowedCrawlUsers = new HashSet<string>() { Address1 },
        });

        return mock.Object;
    }

    private IOptionsMonitor<UserOptions> MockUserOptions()
    {
        var mock = new Mock<IOptionsMonitor<UserOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new UserOptions
        {
            UserSourceList = new List<string>() { "UserSource1", "UserSource2" },
            CheckKey = "CheckKey"
        });
        return mock.Object;
    }

    private IOptionsSnapshot<GraphQLOptions> MockGraphQlOptions()
    {
        var options = new GraphQLOptions()
        {
            Configuration = "http://127.0.0.1:9200"
        };

        var mock = new Mock<IOptionsSnapshot<GraphQLOptions>>();
        mock.Setup(o => o.Value).Returns(options);
        return mock.Object;
    }

    private IOptionsMonitor<ExplorerOptions> MockExplorerOptions()
    {
        var mock = new Mock<IOptionsMonitor<ExplorerOptions>>();
        mock.Setup(o => o.CurrentValue).Returns(new ExplorerOptions
        {
            BaseUrl = new Dictionary<string, string>()
            {
                { "AELF", @"https://explorer.io" },
                { "tDVV", @"https://tdvv-explorer.io" },
                { "tDVW", @"https://tdvw-explorer.io" }
            }
        });
        return mock.Object;
    }

    private static IOptionsSnapshot<QueryContractOption> MockQueryContractOption()
    {
        var mock = new Mock<IOptionsSnapshot<QueryContractOption>>();
        mock.Setup(m => m.Value).Returns(value: new QueryContractOption
        {
            QueryContractInfoList = new List<QueryContractInfo>()
            {
                new QueryContractInfo
                {
                    ChainId = ChainIdAELF,
                    PrivateKey = "PrivateKey",
                    ConsensusContractAddress = "ConsensusContractAddress",
                    ElectionContractAddress = "ElectionContractAddress",
                    GovernanceContractAddress = "GovernanceContractAddress"
                }
            }
        });
        return mock.Object;
    }

    private static IOptionsMonitor<ContractInfoOptions> MockContractInfoOptions()
    {
        var mock = new Mock<IOptionsMonitor<ContractInfoOptions>>();
        mock.Setup(m => m.CurrentValue).Returns(new ContractInfoOptions
        {
            ContractInfos = new Dictionary<string, Dictionary<string, ContractInfo>>()
            {
                {
                    "tDVW", new Dictionary<string, ContractInfo>()
                    {
                        {
                            "2sJ8MDufVDR3V8fDhBPUKMdP84CUf1oJroi9p8Er1yRvMp3fq7", new ContractInfo
                            {
                                ContractAddress = "2sJ8MDufVDR3V8fDhBPUKMdP84CUf1oJroi9p8Er1yRvMp3fq7",
                                ContractName = "TreasuryContract",
                                FunctionList = new List<string>() { "CreateTreasury", "Donate" }
                            }
                        },
                        {
                            "RRF7deQbmicUh6CZ1R2y7U9M8n2eHPyCgXVHwiSkmNETLbL4D", new ContractInfo
                            {
                                ContractAddress = "RRF7deQbmicUh6CZ1R2y7U9M8n2eHPyCgXVHwiSkmNETLbL4D",
                                ContractName = "DAOContract",
                                FunctionList = new List<string>()
                                {
                                    "EnableHighCouncil", "UpdateGovernanceSchemeThreshold",
                                    "RemoveGovernanceScheme", "SetGovernanceToken",
                                    "SetProposalTimePeriod", "SetSubsistStatus",
                                    "AddHighCouncilMembers", "RemoveHighCouncilMembers"
                                },
                                MultiSigDaoMethodBlacklist = new List<string>()
                                {
                                    "EnableHighCouncil", "SetGovernanceToken", "AddHighCouncilMembers",
                                    "RemoveHighCouncilMembers"
                                }
                            }
                        }
                    }
                }
            }
        });
        return mock.Object;
    }

    private IAbpDistributedLock GetMockAbpDistributedLockAlwaysSuccess()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) =>
                Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
        return mockLockProvider.Object;
    }

    private IOptionsMonitor<ChainOptions> MockChainOptions()
    {
        var mock = new Mock<IOptionsMonitor<ChainOptions>>();
        mock.Setup(e => e.CurrentValue).Returns(new ChainOptions
        {
            PrivateKeyForCallTx = PrivateKey1,
            ChainInfos = new Dictionary<string, ChainOptions.ChainInfo>()
            {
                {
                    ChainIdAELF, new ChainOptions.ChainInfo
                    {
                        BaseUrl = "https://test-node.io",
                        IsMainChain = true,
                        ContractAddress = new Dictionary<string, string>()
                        {
                            { "CaAddress", Address1 },
                            { "AElf.ContractNames.Treasury", "AElfTreasuryContractAddress" },
                            { "AElf.ContractNames.Token", "AElfContractNamesToken" },
                            { "VoteContractAddress", "VoteContractAddress" },
                            { "AElf.Contracts.ProxyAccountContract", "ProxyAccountContract" }
                        }
                    }
                },
                {
                    ChainIdTDVV, new ChainOptions.ChainInfo
                    {
                        BaseUrl = "https://test-tdvv-node.io",
                        IsMainChain = false,
                        ContractAddress = new Dictionary<string, string>()
                        {
                            { "CaAddress", "CAContractAddress" },
                            { "TreasuryContractAddress", "TreasuryContractAddress" },
                            { "AElf.ContractNames.Token", "AElfContractNamesToken" },
                            { "VoteContractAddress", "VoteContractAddress" },
                            { "AElf.Contracts.ProxyAccountContract", "ProxyAccountContract" }
                        }
                    }
                },
                {
                    ChainIdtDVW, new ChainOptions.ChainInfo
                    {
                        BaseUrl = "https://test-tdvv-node.io",
                        IsMainChain = false,
                        ContractAddress = new Dictionary<string, string>()
                        {
                            { "CaAddress", Address1 },
                            { "TreasuryContractAddress", TreasuryContractAddress },
                            { "AElf.ContractNames.Token", Address1 },
                            { "VoteContractAddress", Address2 },
                            { "AElf.Contracts.ProxyAccountContract", "ProxyAccountContract" }
                        }
                    }
                }
            },
            TokenImageRefreshDelaySeconds = 300
        });
        return mock.Object;
    }

    private IOptionsSnapshot<ApiOption> MockApiOption()
    {
        var mock = new Mock<IOptionsSnapshot<ApiOption>>();
        mock.Setup(m => m.Value).Returns(new ApiOption
        {
            ChainNodeApis = new Dictionary<string, string>()
            {
                { ChainIdAELF, "https://node.chain.io" }
            }
        });
        return mock.Object;
    }

    private IOptionsMonitor<RankingOptions> MockRankingOptions()
    {
        var mock = new Mock<IOptionsMonitor<RankingOptions>>();

        mock.Setup(m => m.CurrentValue).Returns(new RankingOptions
        {
            TopRankingAddress = Address1,
            PointsLogin = new List<long>() { 100, 100, 250, 100, 100, 100, 250 },
            CustomDaoIds = new List<string>() { CustomDaoId }
        });

        return mock.Object;
    }

    //Login example
    protected void Login(Guid userId, string userAddress = null)
    {
        CurrentUser.Id.Returns(userId);
        CurrentUser.IsAuthenticated.Returns(userId != Guid.Empty);
        var address = userId != Guid.Empty ? (userAddress ?? Address1) : string.Empty;
        var addressCaHash = userId != Guid.Empty ? Address1CaHash : string.Empty;
        UserProviderMock.Setup(o => o.GetAndValidateUserAddressAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Guid userId, string chainId) =>
            {
                if (address.IsNullOrWhiteSpace())
                {
                    throw new UserFriendlyException("No user address found");
                }

                return address;
            });
        UserProviderMock.Setup(o => o.GetAndValidateUserAddressAndCaHashAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Guid userId, string chainId) =>
            {
                if (!address.IsNullOrWhiteSpace() && !addressCaHash.IsNullOrWhiteSpace())
                {
                    return new Tuple<string, string>(address, addressCaHash);
                }

                throw new UserFriendlyException("No user address and caHash found");
            });
        UserProviderMock.Setup(o => o.GetAuthenticatedUserAsync(It.IsAny<ICurrentUser>()))
            .ReturnsAsync((ICurrentUser currentUser) =>
            {
                if (currentUser.Id == null || currentUser.Id == Guid.Empty)
                {
                    throw new UserFriendlyException("User is not authenticated.");
                }

                return new UserGrainDto
                {
                    AppId = null,
                    UserId = CurrentUser.Id ?? Guid.Empty,
                    UserName = "UserName",
                    CaHash = userAddress.IsNullOrWhiteSpace() ? null : Address1CaHash,
                    AddressInfos = new List<AddressInfo>()
                    {
                        new AddressInfo
                        {
                            ChainId = ChainIdAELF,
                            Address = userAddress
                        },
                        new AddressInfo
                        {
                            ChainId = ChainIdtDVW,
                            Address = userAddress
                        },
                        new AddressInfo
                        {
                            ChainId = ChainIdTDVV,
                            Address = userAddress
                        }
                    },
                    CreateTime = 1234567,
                    ModificationTime = 1234568,
                    GuardianIdentifier = "123456789",
                    Address = userAddress,
                    Extra = null,
                    UserInfo = null
                };
            });
        UserProviderMock.Setup(o => o.GetUserAddressAsync(It.IsAny<string>(), It.IsAny<UserGrainDto>()))
            .ReturnsAsync(address);
        if (userId != Guid.Empty)
        {
            UserProviderMock.Setup(o => o.GetAuthenticatedUserAsync(It.IsAny<ICurrentUser>())).ReturnsAsync(
                new UserGrainDto
                {
                    AppId = "TG",
                    UserId = userId,
                    UserName = address,
                    CaHash = "CaHash",
                    AddressInfos = new List<AddressInfo>()
                    {
                        new AddressInfo
                        {
                            ChainId = ChainIdAELF,
                            Address = address
                        },
                        new AddressInfo
                        {
                            ChainId = ChainIdtDVW,
                            Address = address
                        }
                    },
                    CreateTime = DateTime.UtcNow.Date.AddDays(-10).ToUtcMilliSeconds(),
                    ModificationTime = DateTime.UtcNow.ToUtcMilliSeconds(),
                    GuardianIdentifier = "GuardianIdentifier",
                    Address = address,
                    Extra = JsonConvert.SerializeObject(new UserExtraDto
                    {
                        ConsecutiveLoginDays = 0,
                        LastModifiedTime = default,
                        DailyPointsClaimedStatus = new bool[]
                        {
                        },
                        HasVisitedVotePage = false
                    }),
                    UserInfo = JsonConvert.SerializeObject(new TelegramAuthDataDto
                    {
                        Id = "Id",
                        UserName = "UserName",
                        AuthDate = "AuthDate",
                        FirstName = "FirstName",
                        LastName = "LastName",
                        Hash = "Hash",
                        PhotoUrl = "PhotoUrl",
                        BotId = "BotId"
                    })
                });
        }
    }

    protected async Task GenerateReferralCycleAsync()
    {
        await _referralCycleProvider.AddOrUpdateAsync(new ReferralCycleIndex
        {
            Id = "Id",
            ChainId = ChainIdAELF,
            StartTime = DateTime.UtcNow.Date.AddDays(-5).ToUtcMilliSeconds(),
            EndTime = DateTime.UtcNow.Date.AddDays(5).ToUtcMilliSeconds(),
            PointsDistribute = false
        });
    }
}