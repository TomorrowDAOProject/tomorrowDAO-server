using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Vote.Index;
using Volo.Abp.ObjectMapping;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Proposal;

public class ProposalAssistServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IProposalAssistService _proposalAssistService;
    private readonly IObjectMapper _objectMapper;

    public ProposalAssistServiceTest(ITestOutputHelper output) : base(output)
    {
        _proposalAssistService = Application.ServiceProvider.GetRequiredService<ProposalAssistService>();
        _objectMapper = Application.ServiceProvider.GetRequiredService<IObjectMapper>();
    }

    [Fact]
    public async Task ConvertProposalListTest()
    {
        var tuple = await _proposalAssistService.ConvertProposalList(ChainIdAELF, new List<IndexerProposal>()
        {
            GenerateProposal()
        });
        tuple.ShouldNotBeNull();
        tuple.Item1.ShouldNotBeNull();
        tuple.Item1.Count.ShouldBe(1);
        tuple.Item2.ShouldNotBeNull();
        tuple.Item2.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ConvertProposalLifeListTest()
    {
        var proposalIndex = _objectMapper.Map<IndexerProposal, ProposalIndex>(GenerateProposal());
        proposalIndex.ActiveStartTime = DateTime.UtcNow.AddDays(-1);
        proposalIndex.DeployTime = DateTime.UtcNow.AddDays(-2);
        var proposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        proposalLifeList.ShouldNotBeNull();
        proposalLifeList.Count.ShouldBeGreaterThan(1);

        proposalIndex.ProposalStage = ProposalStage.Pending;
        proposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        proposalLifeList.ShouldNotBeNull();
        
        proposalIndex.ProposalStage = ProposalStage.Execute;
        proposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        proposalLifeList.ShouldNotBeNull();

        proposalIndex.ProposalStage = ProposalStage.Finished;
        proposalIndex.ProposalStatus = ProposalStatus.Vetoed;
        proposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        proposalLifeList.ShouldNotBeNull();
        
        proposalIndex.ProposalStage = ProposalStage.Finished;
        proposalIndex.ProposalStatus = ProposalStatus.Rejected;
        proposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        proposalLifeList.ShouldNotBeNull();
        
        proposalIndex.ProposalStage = ProposalStage.Finished;
        proposalIndex.ProposalStatus = ProposalStatus.Executed;
        proposalLifeList = _proposalAssistService.ConvertProposalLifeList(proposalIndex);
        proposalLifeList.ShouldNotBeNull();
    }

    private IndexerProposal GenerateProposal()
    {
        return new IndexerProposal
        {
            Id = "Id",
            ChainId = ChainIdAELF,
            BlockHeight = 100,
            DAOId = CustomDaoId,
            ProposalId = ProposalId1,
            ProposalTitle = "ProposalTitle",
            ProposalDescription = "##GameRanking:crypto-bot",
            ForumUrl = string.Empty,
            ProposalType = ProposalType.Advisory,
            ActiveStartTime = DateTime.UtcNow.AddDays(-1),
            ActiveEndTime = DateTime.UtcNow.AddDays(1),
            ExecuteStartTime = DateTime.UtcNow.AddDays(2),
            ExecuteEndTime = DateTime.UtcNow.AddDays(3),
            ProposalStatus = ProposalStatus.PendingVote,
            ProposalStage = ProposalStage.Active,
            Proposer = Address1,
            SchemeAddress = "SchemeAddress",
            Transaction = new ExecuteTransactionDto
            {
                ToAddress = "ToAddress",
                ContractMethodName = "ContractMethodName",
                Params = string.Empty
            },
            VoteSchemeId = "VoteSchemeId",
            VetoProposalId = null,
            BeVetoProposalId = null,
            DeployTime = DateTime.UtcNow.AddDays(-1),
            ExecuteTime = null,
            GovernanceMechanism = GovernanceMechanism.Organization,
            MinimalRequiredThreshold = 10,
            MinimalVoteThreshold = 10,
            MinimalApproveThreshold = 10,
            MaximalRejectionThreshold = 10,
            MaximalAbstentionThreshold = 10,
            ProposalThreshold = 10,
            ActiveTimePeriod = 0,
            VetoActiveTimePeriod = 0,
            PendingTimePeriod = 0,
            ExecuteTimePeriod = 0,
            VetoExecuteTimePeriod = 0,
            VoteFinished = false,
            IsNetworkDAO = false,
            ProposalCategory = ProposalCategory.Normal,
            RankingType = RankingType.All
        };
    }
}