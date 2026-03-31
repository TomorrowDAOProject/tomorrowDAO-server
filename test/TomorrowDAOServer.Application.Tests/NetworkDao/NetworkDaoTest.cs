using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Provider;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.NetworkDao;

public partial class NetworkDaoTest : TomorrowDaoServerApplicationTestBase
{
    private readonly INetworkDaoVoteService _networkDaoVoteService;
    private readonly INetworkDaoProposalService _networkDaoProposalService;
    private readonly INetworkDaoProposalProvider _networkDaoProposalProvider;
    private readonly INetworkDaoElectionService _networkDaoElectionService;
    private readonly IContractProvider _contractProvider;

    public NetworkDaoTest(ITestOutputHelper output) : base(output)
    {
        _networkDaoVoteService = Application.ServiceProvider.GetRequiredService<INetworkDaoVoteService>();
        _networkDaoProposalService = Application.ServiceProvider.GetRequiredService<NetworkDaoProposalService>();
        _networkDaoProposalProvider = Application.ServiceProvider.GetRequiredService<NetworkDaoProposalProvider>();
        _networkDaoElectionService = Application.ServiceProvider.GetRequiredService<INetworkDaoElectionService>();
        _contractProvider = Application.ServiceProvider.GetRequiredService<IContractProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockGraphQlHelper_NetworkDaoProposalDto());

        //Transaction
        ContractProviderMock.MockTransaction_blockChain_chainStatus();
    }

    [Fact]
    public async Task AddTeamDesc_Should_DefaultUpdateTime_And_BeReadable()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var publicKey = $"vote-team-public-key-{suffix}";
        var address = $"vote-team-address-{suffix}";
        var teamName = $"vote-team-{suffix}";

        Login(Guid.NewGuid(), address);

        var beforeAdd = DateTime.Now;
        var addResult = await _networkDaoVoteService.AddTeamDescriptionAsync(new AddTeamDescInput
        {
            ChainId = ChainIdAELF,
            PublicKey = publicKey,
            Address = address,
            Name = teamName,
            Intro = "team intro"
        });
        var afterAdd = DateTime.Now;

        addResult.Success.ShouldBeTrue();

        var teamDesc = await _networkDaoVoteService.GetTeamDescAsync(new GetTeamDescInput
        {
            ChainId = ChainIdAELF,
            PublicKey = publicKey
        });

        teamDesc.ShouldNotBeNull();
        teamDesc.Name.ShouldBe(teamName);
        teamDesc.PublicKey.ShouldBe(publicKey);
        teamDesc.Address.ShouldBe(address);
        teamDesc.UpdateTime.ShouldNotBeNull();
        teamDesc.UpdateTime.Value.ShouldBeInRange(beforeAdd, afterAdd);
    }

    [Fact]
    public async Task GetTeamDesc_Should_ReturnLatestName_AfterRenamingSamePublicKey()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var publicKey = $"vote-team-public-key-{suffix}";
        var address = $"vote-team-address-{suffix}";
        var originalName = $"vote-team-original-{suffix}";
        var renamedName = $"vote-team-renamed-{suffix}";

        Login(Guid.NewGuid(), address);

        var firstAddResult = await _networkDaoVoteService.AddTeamDescriptionAsync(new AddTeamDescInput
        {
            ChainId = ChainIdAELF,
            PublicKey = publicKey,
            Address = address,
            Name = originalName
        });
        firstAddResult.Success.ShouldBeTrue();

        var originalTeamDesc = await _networkDaoVoteService.GetTeamDescAsync(new GetTeamDescInput
        {
            ChainId = ChainIdAELF,
            PublicKey = publicKey
        });
        originalTeamDesc.Name.ShouldBe(originalName);
        originalTeamDesc.UpdateTime.ShouldNotBeNull();

        await Task.Delay(50);

        var secondAddResult = await _networkDaoVoteService.AddTeamDescriptionAsync(new AddTeamDescInput
        {
            ChainId = ChainIdAELF,
            PublicKey = publicKey,
            Address = address,
            Name = renamedName
        });
        secondAddResult.Success.ShouldBeTrue();

        var renamedTeamDesc = await _networkDaoVoteService.GetTeamDescAsync(new GetTeamDescInput
        {
            ChainId = ChainIdAELF,
            PublicKey = publicKey
        });

        renamedTeamDesc.ShouldNotBeNull();
        renamedTeamDesc.Name.ShouldBe(renamedName);
        renamedTeamDesc.UpdateTime.ShouldNotBeNull();
        renamedTeamDesc.UpdateTime.Value.ShouldBeGreaterThan(originalTeamDesc.UpdateTime.Value);
    }
}
