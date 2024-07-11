using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Options;
using Volo.Abp.DistributedLocking;
using Xunit.Abstractions;
using GraphQLOptions = TomorrowDAOServer.Common.GraphQL.GraphQLOptions;

namespace TomorrowDAOServer;

public abstract partial class
    TomorrowDaoServerApplicationTestBase : TomorrowDAOServerTestBase<TomorrowDAOServerApplicationTestModule>
{
    protected const string ChainIdTDVV = "tDVV";
    protected const string ChainIdAELF = "AELF";
    protected const string ELF = "ELF";
    protected const string ProposalId1 = "99df86594a989227b8e6259f70b08976812537c20486717a3d0158788155b1f0";
    protected const string ProposalId2 = "40510452a04b0857003be9bc222e672c7aff3bf3d4d858a5d72ad2df409b7b6d";
    protected const string DAOId = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";
    protected const string PrivateKey1 = "87ec6028d6c4fa6fd43a1a68c589e737dc8bf4b8968373068dc39a91f70fbeb1";

    protected const string PublicKey1 =
        "04f5db833e5377cab193e3fc663209ac3293ef67736021ee9cebfd1b95a058a5bb400aaeb02ed15dc93177c9bcf38057c4b8069f46601a2180e892a555345c89cf";

    protected const string Address1 = "2Md6Vo6SWrJPRJKjGeiJtrJFVkbc5EARXHGcxJoeD75pMSfdN2";
    protected const string PrivateKey2 = "7f089cb3e5e5045b5a8369b81009b023f67414d53ab94c1d2c44dff6e10005d4";

    protected const string PublicKey2 =
        "04de4367b534d76e8586ac191e611c4ac05064b8bc585449aee19a8818e226ad29c24559216fd33c28abe7acaa8471d2b521152e8b40290dfc420d6eb89f70861a";

    protected const string Address2 = "2DA5orGjmRPJBCDiZQ76NSVrYm7Sn5hwgVui76kCJBMFJYxQFw";

    public TomorrowDaoServerApplicationTestBase(ITestOutputHelper output) : base(output)
    {
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAbpDistributedLockAlwaysSuccess());
        services.AddSingleton(MockGraphQlOptions());
        services.AddSingleton(MockExplorerOptions());
        services.AddSingleton(MockQueryContractOption());
        services.AddSingleton(HttpRequestMock.MockHttpFactory());
        
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

    private IAbpDistributedLock GetMockAbpDistributedLockAlwaysSuccess()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) =>
                Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
        return mockLockProvider.Object;
    }
}