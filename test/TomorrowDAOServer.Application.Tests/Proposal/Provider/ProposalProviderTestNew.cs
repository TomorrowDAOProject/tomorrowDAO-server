using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Linq;
using Shouldly;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.Proposal.Provider;

public partial class ProposalProviderTestNew : TomorrowDaoServerApplicationTestBase
{
    private IProposalProvider _proposalProvider;

    public ProposalProviderTestNew(ITestOutputHelper output) : base(output)
    {
        _proposalProvider = ServiceProvider.GetRequiredService<IProposalProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        //services.AddSingleton(MockHttpProvider());
    }

    [Fact]
    public async Task GetDefaultProposalAsyncTest()
    {
        await _proposalProvider.BulkAddOrUpdateAsync(new List<ProposalIndex>()
        {
            new ProposalIndex
            {
                ChainId = ChainIdAELF,
                BlockHash = null,
                BlockHeight = 0,
                Id = null,
                DAOId = null,
                ProposalId = null,
                ProposalTitle = null,
                ProposalDescription = null,
                ForumUrl = null,
                ProposalType = ProposalType.Unspecified,
                ActiveStartTime = DateTime.UtcNow.AddDays(-1),
                ActiveEndTime = default,
                ExecuteStartTime = default,
                ExecuteEndTime = default,
                ProposalStatus = ProposalStatus.Empty,
                ProposalStage = ProposalStage.Default,
                Proposer = null,
                SchemeAddress = null,
                Transaction = null,
                VoteSchemeId = null,
                VoteMechanism = VoteMechanism.UNIQUE_VOTE,
                VetoProposalId = null,
                BeVetoProposalId = null,
                DeployTime = default,
                ExecuteTime = null,
                GovernanceMechanism = GovernanceMechanism.Referendum,
                MinimalRequiredThreshold = 0,
                MinimalVoteThreshold = 0,
                MinimalApproveThreshold = 0,
                MaximalRejectionThreshold = 0,
                MaximalAbstentionThreshold = 0,
                ProposalThreshold = 0,
                ActiveTimePeriod = 0,
                VetoActiveTimePeriod = 0,
                PendingTimePeriod = 0,
                ExecuteTimePeriod = 0,
                VetoExecuteTimePeriod = 0,
                VoteFinished = false,
                IsNetworkDAO = false,
                ProposalCategory = ProposalCategory.Ranking,
                RankingType = RankingType.All,
                ProposalIcon = null,
                ProposerId = null,
                ProposerFirstName = null,
                ProposalSource = ProposalSourceEnum.TMRWDAO
            }
        });
        var proposalIndex = await _proposalProvider.GetDefaultProposalAsync(ChainIdAELF);
        proposalIndex.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetRankingProposalListAsyncTest()
    {
        var (totalCount, list) = await _proposalProvider.GetRankingProposalListAsync(ChainIdAELF, 0, Int32.MaxValue,
            RankingType.Community,
            excludeAddress: "excludeAddress", needActive: true, excludeProposalIds: new List<string>()
            {
                "123"
            });
        list.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetActiveRankingProposalListAsyncTest()
    {
        var proposalList = await _proposalProvider.GetActiveRankingProposalListAsync(new List<string>() { "DaoId" });
        proposalList.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPollListAsyncTest()
    {
        var (totalCount, list) =
            await _proposalProvider.GetPollListAsync(ChainIdAELF, 0, 100, true, new List<string>() { "proposalIds" });
        list.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProposalCountByDaoIdsTest()
    {
        var dictionary = await _proposalProvider.GetProposalCountByDaoIds(ChainIdAELF, new HashSet<string>() {"DaoIds"});
        dictionary.ShouldNotBeNull();
    }
}