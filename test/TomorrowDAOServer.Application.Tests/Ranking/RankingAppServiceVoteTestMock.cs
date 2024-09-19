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

public partial class RankingAppServiceTest
{
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
                o.GetAsync(It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>()))!
            .ReturnsAsync((string key, bool? _, bool _, CancellationToken _) =>
            {
                if (key.Contains(RankingVoteStatusEnum.Voted.ToString()))
                {
                    return JsonConvert.SerializeObject(new RankingVoteRecord
                    {
                        TransactionId = TransactionHash.ToHex(),
                        VoteTime = DateTime.Now.ToUtcString(),
                        Status = RankingVoteStatusEnum.Voted,
                    });
                }

                if (key.IndexOf(RankingVoteStatusEnum.Voting.ToString(), StringComparison.Ordinal) > 20)
                {
                    return JsonConvert.SerializeObject(new RankingVoteRecord
                    {
                        TransactionId = TransactionHash.ToHex(),
                        VoteTime = DateTime.Now.ToUtcString(),
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