using System;
using System.Threading;
using Moq;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using StackExchange.Redis;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp.Caching;

namespace TomorrowDAOServer.Ranking.Provider;

public partial class RankingAppPointsRedisProviderTest
{
    public static IConnectionMultiplexer MockConnectionMultiplexer()
    {
        var mock = new Mock<IConnectionMultiplexer>();

        mock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns((int db, object asyncState) =>
        {
            return MockDatabase();
        });
        return mock.Object;
    }

    public static IDatabase MockDatabase()
    {
        var mock = new Mock<IDatabase>();

        mock.Setup(m => m.KeyExists(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).Returns(
            (RedisKey key, CommandFlags flags) => { return true; });

        // mock.Setup(m => m.StringSetAsync(It.IsAny<KeyValuePair<RedisKey, RedisValue>>(), It.IsAny<When>(),
        //         It.IsAny<CommandFlags>()))
        //     .ReturnsAsync((KeyValuePair<RedisKey, RedisValue>[] values, When when, CommandFlags flags) => { return true; });

        mock.Setup(m => m.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>())).ReturnsAsync(
            (RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags) => { return true; });

        mock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>())).ReturnsAsync(
            (RedisKey[] keys, CommandFlags flags) => { return new RedisValue[] { new("1") }; });

        mock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(
            (RedisKey key, CommandFlags flags) =>
            {
                if (key.ToString() == RedisHelper.GenerateDefaultProposalCacheKey(ChainIdAELF))
                {
                    return new RedisValue(ProposalId1);
                }

                return new("2");
            });

        mock.Setup(m => m.CreateBatch(It.IsAny<object>())).Returns(MockBatch());

        return mock.Object;
    }

    private static IBatch MockBatch()
    {
        var mock = new Mock<IBatch>();

        mock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("10"));

        //mock.Setup(m => m.Execute()).ReturnsNull();

        return mock.Object;
    }

    public static IDistributedCache<string> MockDistributedCache()
    {
        var mock = new Mock<IDistributedCache<string>>();

        mock.Setup(m =>
                m.GetAsync(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, bool? hideErrors, bool considerUow, CancellationToken token) =>
            {
                if (key.StartsWith("RankingVotingRecord:"))
                {
                    return JsonConvert.SerializeObject(new RankingVoteRecord
                    {
                        TransactionId = "TransactionId",
                        VoteTime = DateTime.UtcNow.ToString(),
                        Status = RankingVoteStatusEnum.Voted,
                        TotalPoints = 1
                    });
                }
                return new RedisValue($"{ProposalId1},Alias");
            });

        return mock.Object;
    }
}