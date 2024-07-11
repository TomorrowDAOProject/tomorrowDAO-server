using Moq;
using TomorrowDAOServer.Common.AElfSdk;

namespace TomorrowDAOServer.Common.Mocks;

public class ContractProviderMock
{
    public static IContractProvider MockContractProvider()
    {
        var mock = new Mock<IContractProvider>();

        return mock.Object;
    }
}