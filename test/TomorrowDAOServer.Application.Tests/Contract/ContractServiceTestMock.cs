using Moq;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Contract;

public partial class ContractServiceTest
{
    private IDAOProvider MockDaoProvider()
    {
        var mock = new Mock<IDAOProvider>();
        mock.Setup(m => m.GetAsync(It.IsAny<GetDAOInfoInput>())).ReturnsAsync(new DAOIndex
        {
            Id = "DaoId",
            ChainId = ChainIdtDVW,
            
            GovernanceMechanism = GovernanceMechanism.Organization
        });
        return mock.Object;
    } 
}