using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Treasury;

public partial class TreasuryAssetsServiceTest : TomorrowDaoServerApplicationTestBase
{
    private static IDAOProvider MockDaoProvider()
    {
        var mock = new Mock<IDAOProvider>();

        mock.Setup(o => o.GetAsync(It.IsAny<GetDAOInfoInput>())).ReturnsAsync((GetDAOInfoInput input) =>
        {
            var daoIndex = new DAOIndex
            {
                Id = input.DAOId,
                ChainId = input.ChainId,
                Alias = input.DAOId,
                AliasHexString = null,
                BlockHeight = 0,
                Creator = null,
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
            if (input.DAOId.IndexOf("NetworkDao") != -1)
            {
                daoIndex.IsNetworkDAO = true;
            }
            else
            {
                daoIndex.IsNetworkDAO = false;
            }
            return daoIndex;
        });

        return mock.Object;
    }

    private static INetworkDaoTreasuryService MockNetworkDaoTreasuryService()
    {
        var mock = new Mock<INetworkDaoTreasuryService>();
        mock.Setup(o => o.GetBalanceAsync(It.IsAny<TreasuryBalanceRequest>()))
            .ReturnsAsync(new TreasuryBalanceResponse
            {
                ContractAddress = "ContractAddress",
                Items = new List<TreasuryBalanceResponse.BalanceItem>() {new TreasuryBalanceResponse.BalanceItem
                    {
                        TotalCount = "10",
                        DollarValue = "50.13",
                        Token = new TokenDto
                        {
                            Symbol = "ELF",
                            TotalSupply = "100000000",
                            Supply = "1000000",
                            Name = "AELF",
                            Decimals = 8,
                            ChainId = ChainIdAELF,
                            ImageUrl = "ImageUrl"
                        }
                    }
                }
            });

        return mock.Object;
    }
}