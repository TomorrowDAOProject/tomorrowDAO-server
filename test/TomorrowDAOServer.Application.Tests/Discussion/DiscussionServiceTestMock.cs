using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Discussion.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Provider;
using Xunit;
using Xunit.Abstractions;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.Discussion;

public partial class DiscussionServiceTest
{
    private IProposalProvider MockProposalProvider()
    {
        var mock = new Mock<IProposalProvider>();

        mock.Setup(m => m.GetProposalByIdAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string chainId, string proposalId) =>
        {
            return new ProposalIndex
            {
                ChainId = ChainIdAELF,
                BlockHash = null,
                BlockHeight = 0,
                Id = null,
                DAOId = DAOId,
                ProposalId = proposalId,
                ProposalTitle = "null",
                ProposalDescription = "null",
                ForumUrl = null,
                ProposalType = ProposalType.Unspecified,
                ActiveStartTime = default,
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
                ProposalCategory = ProposalCategory.Normal,
                RankingType = RankingType.All,
                ProposalIcon = null,
                ProposalSource = ProposalSourceEnum.TMRWDAO
            };
        });
        return mock.Object;
    }

    private IDAOProvider MockDaoProvider()
    {
        var mock = new Mock<IDAOProvider>();

        mock.Setup(m => m.GetAsync(It.IsAny<GetDAOInfoInput>())).ReturnsAsync((GetDAOInfoInput input) =>
        {
            return new DAOIndex
            {
                Id = DAOId,
                ChainId = ChainIdAELF,
                Alias = null,
                AliasHexString = null,
                BlockHeight = 0,
                Creator = null,
                Metadata = null,
                GovernanceToken = ELF,
                IsHighCouncilEnabled = false,
                HighCouncilAddress = null,
                HighCouncilConfig = null,
                HighCouncilTermNumber = 0,
                FileInfoList = null,
                IsTreasuryContractNeeded = false,
                SubsistStatus = false,
                TreasuryContractAddress = null,
                TreasuryAccountAddress = null,
                IsTreasuryPause = false,
                TreasuryPauseExecutor = null,
                VoteContractAddress = null,
                ElectionContractAddress = null,
                GovernanceContractAddress = null,
                TimelockContractAddress = null,
                ActiveTimePeriod = 0,
                VetoActiveTimePeriod = 0,
                PendingTimePeriod = 0,
                ExecuteTimePeriod = 0,
                VetoExecuteTimePeriod = 0,
                CreateTime = default,
                IsNetworkDAO = false,
                VoterCount = 0,
                GovernanceMechanism = GovernanceMechanism.Referendum
            };
        });

        return mock.Object;
    }
}