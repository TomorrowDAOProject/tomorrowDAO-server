using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Moq;
using Nest;
using NSubstitute;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Vote;
using static TomorrowDAOServer.Common.TestConstant;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.Proposal;

public partial class ProposalServiceTest
{
    public IDAOProvider MockDaoProvider()
    {
        var daoIndex = new DAOIndex
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

        var mock = new Mock<IDAOProvider>();
        mock.Setup(o => o.GetAsync(It.IsAny<GetDAOInfoInput>())).ReturnsAsync((GetDAOInfoInput input) =>
        {
            if (input != null && input.DAOId.Equals("notexist"))
            {
                return null;
            }

            return daoIndex;
        });
        mock.Setup(o => o.GetDaoListByDaoIds(It.IsAny<string>(), It.IsAny<List<string>>())).ReturnsAsync(
            (string chainId, List<string> daoIds) =>
            {
                return new List<DAOIndex>()
                {
                    daoIndex
                };
            });
        return mock.Object;
    }

    public IProposalProvider MockProposalProvider()
    {
        var proposalIndex = new ProposalIndex
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
            VoteSchemeId = "VoteSchemeId",
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
        };

        var mock = new Mock<IProposalProvider>();
        mock.Setup(o => o.GetProposalListAsync(It.IsAny<QueryProposalListInput>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex>()
            {
                proposalIndex
            }));

        mock.Setup(o => o.GetProposalByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string chainId, string proposalId) =>
            {
                if (proposalId == "notexist")
                {
                    return null;
                }

                return proposalIndex;
            });

        mock.Setup(o => o.GetProposalByIdsAsync(It.IsAny<string>(), It.IsAny<List<string>>())).ReturnsAsync(
            (string chainId, List<string> proposalIds) =>
            {
                return new List<ProposalIndex>()
                {
                    proposalIndex
                };
            });
        mock.Setup(o => o.QueryProposalsByProposerAsync(It.IsAny<QueryProposalByProposerRequest>())).ReturnsAsync(
            (QueryProposalByProposerRequest request) =>
            {
                if (request.DaoId == "notexist")
                {
                    return null;
                }

                return new Tuple<long, List<ProposalIndex>>(1, new List<ProposalIndex>()
                {
                    proposalIndex
                });
            });

        return mock.Object;
    }

    public INESTRepository<VoteRecordIndex, string> MockVoteRecordIndexRepository()
    {
        var mock = new Mock<INESTRepository<VoteRecordIndex, string>>();
        mock.Setup(o => o.GetSortListAsync(It.IsAny<Func<QueryContainerDescriptor<VoteRecordIndex>, QueryContainer>>(),
            It.IsAny<Func<SourceFilterDescriptor<VoteRecordIndex>, ISourceFilter>>(),
            It.IsAny<Func<SortDescriptor<VoteRecordIndex>, IPromise<IList<ISort>>>>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>())).ReturnsAsync(new Tuple<long, List<VoteRecordIndex>>(1, new List<VoteRecordIndex>()
        {
            new VoteRecordIndex
            {
                Id = "Id",
                BlockHeight = 100,
                ChainId = ChainIdAELF,
                TransactionId = TransactionHash.ToHex(),
                DAOId = DaoId,
                VotingItemId = "ProposalId",
                Voter = Address1,
                VoteMechanism = VoteMechanism.TOKEN_BALLOT,
                Amount = 1,
                Option = VoteOption.Approved,
                VoteTime = DateTime.UtcNow.AddMinutes(-1),
                IsWithdraw = false,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(10),
                Memo = string.Empty,
                ValidRankingVote = false,
                Alias = "Alias",
                Title = "Title",
                TotalRecorded = false
            }
        }));
        return mock.Object;
    }
}