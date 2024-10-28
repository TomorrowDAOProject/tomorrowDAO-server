using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Election.Index;
using TomorrowDAOServer.Election.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Governance.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.DAO;

public partial class DaoAppServiceTest
{
    private void MockGetMyOwneredDAOListAsync(IDAOProvider daoProvider)
    {
        daoProvider.GetMyOwneredDAOListAsync(Arg.Any<QueryMyDAOListInput>(), Arg.Any<string>())
            .Returns(new Tuple<long, List<DAOIndex>>(1, new List<DAOIndex>()
            {
                new DAOIndex
                {
                    Id = DAOId,
                    ChainId = ChainIdtDVW,
                    Alias = DAOId,
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
                }
            }));
    }

    private void MockGetMyParticipatedDaoListAsync(IDAOProvider daoProvider)
    {
        daoProvider.GetMyParticipatedDaoListAsync(Arg.Any<GetParticipatedInput>())
            .Returns(new PageResultDto<IndexerDAOInfo>
            {
                TotalCount = 0,
                Data = new List<IndexerDAOInfo>()
                {
                    new IndexerDAOInfo
                    {
                        Id = DAOId,
                        ChainId = ChainIdtDVW,
                        BlockHeight = 0,
                        Creator = null,
                        Metadata = null,
                        GovernanceToken = ELF,
                        IsHighCouncilEnabled = false,
                        HighCouncilAddress = null,
                        MaxHighCouncilMemberCount = 0,
                        MaxHighCouncilCandidateCount = 0,
                        ElectionPeriod = 0,
                        StakingAmount = 0,
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
                        PermissionAddress = null,
                        PermissionInfoList = null,
                        CreateTime = default,
                        IsNetworkDAO = false,
                        VoterCount = 0,
                        GovernanceMechanism = GovernanceMechanism.Referendum
                    }
                }
            });
    }

    private void MockGetManagedDAOAsync(IDAOProvider daoProvider)
    {
        daoProvider.GetManagedDAOAsync(Arg.Any<QueryMyDAOListInput>(), Arg.Any<List<string>>(), Arg.Any<bool>())
            .Returns(new Tuple<long, List<DAOIndex>>(1, new List<DAOIndex>()
            {
                new DAOIndex
                {
                    Id = DAOId,
                    ChainId = ChainIdtDVW,
                    Alias = DAOId,
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
                }
            }));
    }

    private void MockGetDaoListByDaoIds(IDAOProvider daoProvider)
    {
        daoProvider.GetDaoListByDaoIds(Arg.Any<string>(), Arg.Any<List<string>>())
            .Returns(new List<DAOIndex>()
            {
                new DAOIndex
                {
                    Id = DAOId,
                    ChainId = ChainIdtDVW,
                    Alias = DAOId,
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
                }
            });
    }

    private void MockGetTokenInfoAsync(ITokenService tokenService)
    {
        tokenService.GetTokenInfoAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new TokenInfoDto
            {
                Id = "Id",
                ContractAddress = "ContractAddress",
                Symbol = ELF,
                ChainId = ChainIdAELF,
                IssueChainId = ChainIdAELF,
                TxId = null,
                Name = null,
                TotalSupply = null,
                Supply = null,
                Decimals = "8",
                Holders = "10000",
                Transfers = null,
                LastUpdateTime = DateTime.Now.ToUtcMilliSeconds()
            });
    }

    private void MockGetHighCouncilManagedDaoIndexAsync(IElectionProvider electionProvider)
    {
        electionProvider.GetHighCouncilManagedDaoIndexAsync(Arg.Any<GetHighCouncilMemberManagedDaoInput>())
            .Returns(new List<HighCouncilManagedDaoIndex>() { new HighCouncilManagedDaoIndex
                {
                    Id = "Id",
                    MemberAddress = Address1,
                    DaoId = DaoId,
                    ChainId = ChainIdAELF,
                    CreateTime = DateTime.Now
                }
            });
    }
    
    private void MockGetMemberAsync(IDAOProvider daoProvider)
    {
        daoProvider.GetMemberAsync(Arg.Any<GetMemberInput>())
            .Returns(new MemberDto
            {
                Id = "id",
                ChainId = ChainIdAELF,
                BlockHeight = 1000,
                DAOId = DaoId,
                Address = Address1,
                CreateTime = default
            });
    }
}