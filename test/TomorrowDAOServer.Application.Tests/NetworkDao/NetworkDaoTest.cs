using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.NetworkDao.Provider;
using Xunit.Abstractions;

namespace TomorrowDAOServer.NetworkDao;

public partial class NetworkDaoTest : TomorrowDaoServerApplicationTestBase
{
    private readonly INetworkDaoProposalService _networkDaoProposalService;
    private readonly INetworkDaoProposalProvider _networkDaoProposalProvider;

    public NetworkDaoTest(ITestOutputHelper output) : base(output)
    {
        _networkDaoProposalService = Application.ServiceProvider.GetRequiredService<NetworkDaoProposalService>();
        _networkDaoProposalProvider = Application.ServiceProvider.GetRequiredService<NetworkDaoProposalProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockGraphQlHelper_NetworkDaoProposalDto());
        services.AddSingleton(ContractProviderMock.MockContractProvider());
    }
}
