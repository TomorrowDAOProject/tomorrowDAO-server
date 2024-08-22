using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using TomorrowDAO.Contracts.Vote;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Token.Dto;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Ranking;

public partial class RankingAppServiceVoteTest
{
    private IOptionsMonitor<RankingOptions> MockRankingOptions()
    {
        var mock = new Mock<IOptionsMonitor<RankingOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new RankingOptions
        {
            DaoIds = new List<string>() { DAOId },
            DescriptionPattern = string.Empty,
            DescriptionBegin = string.Empty,
            LockUserTimeout = 60000,
            VoteTimeout = 60000,
            RetryTimes = 30,
            RetryDelay = 2000
        });

        return mock.Object;
    }

    private IAbpDistributedLock MockAbpDistributedLock()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) =>
                Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
        return mockLockProvider.Object;
    }

    private IDistributedCache<string> MockIDistributedCache()
    {
        var mock = new Mock<IDistributedCache<string>>();

        mock.Setup(o =>
                o.GetAsync(It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, bool? hideErrors, bool considerUow, CancellationToken token) =>
            {
                if (key.IndexOf(RankingVoteStatusEnum.Voted.ToString()) != -1)
                {
                    return JsonConvert.SerializeObject(new RankingVoteRecord
                    {
                        TransactionId = TransactionHash.ToHex(),
                        VoteTime = TimeHelper.ToUtcString(DateTime.Now),
                        Status = RankingVoteStatusEnum.Voted,
                    });
                }
                else if (key.IndexOf(RankingVoteStatusEnum.Voting.ToString()) > 20)
                {
                    return JsonConvert.SerializeObject(new RankingVoteRecord
                    {
                        TransactionId = TransactionHash.ToHex(),
                        VoteTime = TimeHelper.ToUtcString(DateTime.Now),
                        Status = RankingVoteStatusEnum.Voting,
                    });
                }

                return null;
            });

        return mock.Object;
    }

    private Transaction GeneratePortkeyTransaction()
    { 
        var transaction = new Transaction
        {
            From = Address.FromBase58(Address1),
            To = Address.FromBase58(Address1),
            RefBlockNumber = 100,
            MethodName = "ManagerForwardCall",
            Params = new ManagerForwardCallInput
            {
                CaHash = Hash.LoadFromHex(ProposalId1),
                ContractAddress = Address.FromBase58(Address2),
                MethodName = "Vote",
                Args = new VoteInput
                {
                    VotingItemId = Hash.LoadFromHex(ProposalId1),
                    VoteOption = 1,
                    VoteAmount = 1,
                    Memo = "##GameRanking:{tg-app}"
                }.ToByteString()
            }.ToByteString()
        };
        transaction.Signature = transaction.ToByteString();
        return transaction;
    }
}