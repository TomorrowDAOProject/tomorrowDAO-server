using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Ranking.Dto;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.MQ;

public partial class MessagePublisherServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IMessagePublisherService _messagePublisherService;

    public MessagePublisherServiceTest(ITestOutputHelper output) : base(output)
    {
        _messagePublisherService = Application.ServiceProvider.GetRequiredService<IMessagePublisherService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockDistributedEventBus());
    }

    [Fact]
    public async Task SendLikeMessageAsync()
    {
        await _messagePublisherService.SendLikeMessageAsync(ChainIdAELF, ProposalId1, Address1,
            new List<RankingAppLikeDetailDto>());

        await _messagePublisherService.SendLikeMessageAsync(ChainIdAELF, ProposalId1, Address1,
            new List<RankingAppLikeDetailDto>()
            {
                new RankingAppLikeDetailDto
                {
                    Alias = "Alias",
                    LikeAmount = 10
                }
            });

        await _messagePublisherService.SendLikeMessageAsync(ChainIdAELF, ProposalId1, Address1,
            new List<RankingAppLikeDetailDto>()
            {
                new RankingAppLikeDetailDto
                {
                    Alias = "ThrowException",
                    LikeAmount = 10
                }
            });
    }

    [Fact]
    public async Task SendVoteMessageAsyncTest()
    {
        await _messagePublisherService.SendVoteMessageAsync(ChainIdAELF, ProposalId1, Address1, "Alias", 1);
        await _messagePublisherService.SendVoteMessageAsync(ChainIdAELF, ProposalId1, Address1, "ThrowException", 1);
    }

    [Fact]
    public async Task SendReferralFirstVoteMessageAsyncTest()
    {
        await _messagePublisherService.SendReferralFirstVoteMessageAsync(ChainIdAELF, "inviter", "invitee");
        await _messagePublisherService.SendReferralFirstVoteMessageAsync(ChainIdAELF, "ThrowException", "ThrowException");
    }
}