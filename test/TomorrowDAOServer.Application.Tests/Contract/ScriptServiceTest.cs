using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Contract;

public partial class ScriptServiceTest: TomorrowDaoServerApplicationTestBase
{
    private readonly IScriptService _scriptService;
    
    public ScriptServiceTest(ITestOutputHelper output) : base(output)
    {
        _scriptService = Application.ServiceProvider.GetRequiredService<IScriptService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockTransactionService());
    }
    
    [Fact]
    public async Task GetCurrentBpAsyncTest()
    {
        var currentBp = await _scriptService.GetCurrentBPAsync(ChainIdAELF);
        currentBp.ShouldNotBeNull();
        currentBp.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetCurrentBPRoundAsyncTest()
    {
        var currentBpRound = await _scriptService.GetCurrentBPRoundAsync(ChainIdAELF);
        currentBpRound.ShouldBe(10);
    }

    [Fact]
    public async Task GetCurrentHCAsyncTest()
    {
        var currentHc = await _scriptService.GetCurrentHCAsync(ChainIdAELF, DAOId);
        currentHc.ShouldNotBeNull();
        currentHc.Count.ShouldBe(2);
        currentHc.ShouldContain(Address1);
    }

    [Fact]
    public async Task GetProposalInfoAsyncTest()
    {
        var getProposalInfoDto = await _scriptService.GetProposalInfoAsync(ChainIdAELF, ProposalId1);
        getProposalInfoDto.ShouldNotBeNull();
        getProposalInfoDto.ProposalStage.ShouldBe(ProposalStage.Active.ToString());
        getProposalInfoDto.ProposalStatus.ShouldBe(ProposalStatus.Approved.ToString());

        getProposalInfoDto = await _scriptService.GetProposalInfoAsync("Invalid", ProposalId1);
        getProposalInfoDto.ShouldBeNull();
    }
}