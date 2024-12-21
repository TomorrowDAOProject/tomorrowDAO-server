using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Provider;
using static TomorrowDAOServer.Common.TestConstant;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.Proposal;

public partial class ProposalServiceTest
{
    public IDAOProvider MockDaoProvider()
    {
        var mock = new Mock<IDAOProvider>();
        mock.Setup(o => o.GetAsync(It.IsAny<GetDAOInfoInput>())).ReturnsAsync((GetDAOInfoInput input) =>
        {
            if (input != null && input.DAOId.Equals("notexist"))
            {
                return null;
            }

            return new DAOIndex
            {
                Id = DaoId,
                ChainId = ChainIdAELF,
                Alias = "daoid",
                AliasHexString = null,
                BlockHeight = 100,
                Creator = Address1,
                Metadata = null,
                GovernanceToken = null,
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

    public IProposalProvider MockProposalProvider()
    {
        var mock = new Mock<IProposalProvider>();
        mock.Setup(o => o.GetProposalListAsync(It.IsAny<QueryProposalListInput>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex>()
            {
                new ProposalIndex
                {
                    ChainId = ChainIdAELF,
                    BlockHash = "BlockHash",
                    BlockHeight = 100,
                    Id = "Id",
                    DAOId = DaoId,
                    ProposalId = "ProposalId",
                    ProposalTitle = "ProposalTitle",
                    ProposalDescription = "ProposalDescription",
                    ForumUrl = "ForumUrl",
                    ProposalType = ProposalType.Governance,
                    ActiveStartTime = DateTime.UtcNow.Date.AddDays(-2),
                    ActiveEndTime = DateTime.UtcNow.Date.AddDays(2),
                    ExecuteStartTime = DateTime.UtcNow.Date.AddDays(2),
                    ExecuteEndTime = DateTime.UtcNow.Date.AddDays(4),
                    ProposalStatus = ProposalStatus.Approved,
                    ProposalStage = ProposalStage.Pending,
                    Proposer = Address1,
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
                    ProposerId = null,
                    ProposerFirstName = null,
                    ProposalSource = ProposalSourceEnum.TMRWDAO
                }
            }));
        return mock.Object;
    }
}