using System;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Ranking.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Ranking;

public partial class RankingAppServiceTest
{
    [Fact]
    public async Task VoteAsyncTest()
    {
        var userId = Guid.NewGuid();
        Login(userId);
        var transaction = GeneratePortkeyTransaction();
        await _rankingAppService.VoteAsync(new RankingVoteInput
        {
            ChainId = ChainIdAELF,
            RawTransaction = transaction.ToByteArray().ToHex()
        });
    }
    
    [Fact]
    public async Task VoteAsyncTest_Voted()
    {
        var userId = Guid.NewGuid();
        Login(userId, RankingVoteStatusEnum.Voted.ToString());
        var transaction = GeneratePortkeyTransaction();
        var voteResponse = await _rankingAppService.VoteAsync(new RankingVoteInput
        {
            ChainId = ChainIdAELF,
            RawTransaction = transaction.ToByteArray().ToHex()
        });
        voteResponse.ShouldNotBeNull();
        voteResponse.Status.ShouldBe(RankingVoteStatusEnum.Voted);
    }

    [Fact]
    public async Task VoteAsyncTest_InvalidInput()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _rankingAppService.VoteAsync(new RankingVoteInput
            {
                ChainId = ChainIdAELF,
                RawTransaction = null
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Invalid input.");
    }
    
    [Fact]
    public async Task VoteAsyncTest_NotLoggedIn()
    {
        Login(Guid.Empty);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _rankingAppService.VoteAsync(new RankingVoteInput
            {
                ChainId = ChainIdAELF,
                RawTransaction = new Transaction
                {
                    From = Address.FromBase58(Address1)
                }.ToByteArray().ToHex()
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("User Address Not Found.");
    }
}