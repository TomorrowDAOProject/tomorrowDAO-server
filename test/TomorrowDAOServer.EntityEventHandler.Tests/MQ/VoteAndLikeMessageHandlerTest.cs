using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.EntityEventHandler.Core.MQ;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Ranking.Eto;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.EntityEventHandler.Tests.MQ;

public class VoteAndLikeMessageHandlerTest : TomorrowDAOServerEntityEventHandlerTestsBase
{
    private readonly VoteAndLikeMessageHandler _voteAndLikeMessageHandler;
    public VoteAndLikeMessageHandlerTest(ITestOutputHelper output) : base(output)
    {
        _voteAndLikeMessageHandler = Application.ServiceProvider.GetRequiredService<VoteAndLikeMessageHandler>();
    }

    [Fact]
    public async Task Test()
    {
        Assert.True(true);
    }

    [Fact]
    public async Task HandleEventAsyncTest()
    {
        await _voteAndLikeMessageHandler.HandleEventAsync(new VoteAndLikeMessageEto
        {
            ChainId = ChainIdAELF,
            DaoId = DAOId,
            ProposalId = ProposalId3,
            AppId = "AppId",
            Alias = "Alias",
            Title = "Title",
            Address = Address1,
            Amount = 10,
            PointsType = PointsType.Vote,
            UserId = "UserIdx"
        });
    }
}