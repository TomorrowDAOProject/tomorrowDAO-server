using System.Net.Http;
using AElf.Contracts.Election;
using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.NetworkDao.Provider;
using Xunit.Abstractions;

namespace TomorrowDAOServer.NetworkDao;

public partial class NetworkDaoTest : TomorrowDaoServerApplicationTestBase
{
    private readonly INetworkDaoProposalService _networkDaoProposalService;
    private readonly INetworkDaoProposalProvider _networkDaoProposalProvider;
    private readonly INetworkDaoElectionService _networkDaoElectionService;
    private readonly IContractProvider _contractProvider;

    public NetworkDaoTest(ITestOutputHelper output) : base(output)
    {
        _networkDaoProposalService = Application.ServiceProvider.GetRequiredService<NetworkDaoProposalService>();
        _networkDaoProposalProvider = Application.ServiceProvider.GetRequiredService<NetworkDaoProposalProvider>();
        _networkDaoElectionService = Application.ServiceProvider.GetRequiredService<INetworkDaoElectionService>();
        _contractProvider = Application.ServiceProvider.GetRequiredService<ContractProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockGraphQlHelper_NetworkDaoProposalDto());

        //Transaction
        ContractProviderMock.MockTransaction_blockChain_chainStatus();
    }
}
