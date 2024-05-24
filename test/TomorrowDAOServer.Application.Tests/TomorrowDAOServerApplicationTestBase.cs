using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Entities;
using Volo.Abp.DistributedLocking;
using Xunit.Abstractions;

namespace TomorrowDAOServer;

public abstract partial class TomorrowDaoServerApplicationTestBase : TomorrowDAOServerTestBase<TomorrowDAOServerApplicationTestModule>
{
    protected const string ChainIdTDVV = "tDVV";
    protected const string ChainIdAELF = "AELF";
    protected const string ELF = "ELF";
    protected const string ProposalId = "99df86594a989227b8e6259f70b08976812537c20486717a3d0158788155b1f0";
    protected const string DAOId = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3";
    protected const string ProposalListJson = @"[
        {
            ""id"": ""99df86594a989227b8e6259f70b08976812537c20486717a3d0158788155b1f0"",
            ""daoId"": ""a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3"",
            ""proposalId"": ""99df86594a989227b8e6259f70b08976812537c20486717a3d0158788155b1f0"",
            ""proposalTitle"": ""Proposal Title test"",
            ""proposalType"": 1,
            ""governanceMechanism"": 3,
            ""proposalStatus"": 6,
            ""startTime"": ""2024-02-07T10:10:27.3577550Z"",
            ""endTime"": ""2024-02-09T10:10:27.3580530Z"",
            ""expiredTime"": ""2024-02-10T10:10:27.3580960Z"",
            ""executeTime"": ""2024-02-10T10:05:27.3580960Z"",
            ""executeAddress"": ""aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3"",
            ""proposalDescription"": ""f5bc4667d8cb512113dc140163c5b3bc4829468f49c01483aa46b21298221774"",
            ""transactionInfo"": {
                ""toAddress"": ""YeCqKprLBGbZZeRTkN1FaBLXsetY8QFotmVKqo98w9K6jK2PY"",
                ""contractMethodName"": ""AddMembers"",
                ""params"": {}
            },
            ""governanceSchemeId"": ""f16f5443dbfc30be571104872d88101705834ffeea6632858bc8e70608be5e50"",
            ""executeByHighCouncil"": false,
            ""deployTime"": ""2024-02-07T10:10:27.3691230Z"",
            ""voteFinished"": true,
            ""voteSchemeId"": ""1"",
            ""organizationAddress"": ""UE6mcinaCFJZmGNgY9fpMnyzwMETJUhqwbnvtjRgX1f12rBQj"",
            ""minimalRequiredThreshold"": 11,
            ""minimalVoteThreshold"": 13,
            ""minimalApproveThreshold"": 50,
            ""maximalRejectionThreshold"": 30,
            ""maximalAbstentionThreshold"": 20,
            ""chainId"": ""tDVV"",
            ""blockHash"": ""dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1"",
            ""blockHeight"": 120,
            ""previousBlockHash"": ""e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e"",
            ""isDeleted"": false
        },
        {
            ""id"": ""b97db4a9f43296157fb1a5d38cebdac478d0e91ed7b8dc1ae2effe1e29e64354"",
            ""daoId"": ""a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3"",
            ""proposalId"": ""b97db4a9f43296157fb1a5d38cebdac478d0e91ed7b8dc1ae2effe1e29e64354"",
            ""proposalTitle"": ""Proposal Title test 2"",
            ""proposalType"": 2,
            ""governanceMechanism"": 1,
            ""proposalStatus"": 1,
            ""startTime"": ""2024-02-07T10:03:03.8204790Z"",
            ""endTime"": ""2024-02-09T10:03:03.8207190Z"",
            ""expiredTime"": ""2024-02-10T10:03:03.8207570Z"",
            ""executeAddress"": ""aLyxCJvWMQH6UEykTyeWAcYss9baPyXkrMQ37BHnUicxD2LL3"",
            ""proposalDescription"": ""f5bc4667d8cb512113dc140163c5b3bc4829468f49c01483aa46b21298221774"",
            ""transactionInfo"": {
                ""toAddress"": ""YeCqKprLBGbZZeRTkN1FaBLXsetY8QFotmVKqo98w9K6jK2PY"",
                ""contractMethodName"": ""HighCouncilConfigSet"",
                ""params"": {}
            },
            ""governanceSchemeId"": ""f16f5443dbfc30be571104872d88101705834ffeea6632858bc8e70608be5e50"",
            ""executeByHighCouncil"": false,
            ""deployTime"": ""2024-02-07T10:03:03.8310160Z"",
            ""voteFinished"": false,
            ""voteSchemeId"": ""2"",
            ""organizationAddress"": ""UE6mcinaCFJZmGNgY9fpMnyzwMETJUhqwbnvtjRgX1f12rBQj"",
            ""minimalRequiredThreshold"": 11,
            ""minimalVoteThreshold"": 13,
            ""minimalApproveThreshold"": 50,
            ""maximalRejectionThreshold"": 30,
            ""maximalAbstentionThreshold"": 20,
            ""chainId"": ""tDVV"",
            ""blockHash"": ""dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1"",
            ""blockHeight"": 120,
            ""previousBlockHash"": ""e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e"",
            ""isDeleted"": false
        }   
    ]";
    
    public TomorrowDaoServerApplicationTestBase(ITestOutputHelper output) : base(output)
    {
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAbpDistributedLockAlwaysSuccess());
        services.AddSingleton(MockGraphQlOptions());
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

    private IAbpDistributedLock GetMockAbpDistributedLockAlwaysSuccess()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) => 
                Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
        return mockLockProvider.Object;
    }
    
    protected static IGraphQlHelper MockGraphQlHelper()
    {
        var mockHelper = new Mock<IGraphQlHelper>();
        return mockHelper.Object;
    }
}