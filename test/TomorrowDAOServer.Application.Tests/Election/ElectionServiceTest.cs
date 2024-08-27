using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Election.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Election;

public partial class ElectionServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IElectionService _electionService;
    public ElectionServiceTest(ITestOutputHelper output) : base(output)
    {
        _electionService = GetRequiredService<IElectionService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetIClusterClient());
    }

    [Fact]
    public async Task GetHighCouncilMembersAsyncTest()
    {
        var result = await _electionService.GetHighCouncilMembersAsync(new HighCouncilMembersInput
        {
            ChainId = ChainIdAELF,
            DaoId = "DaoId"
        });
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe("address1");
    }

    [Fact]
    public async Task GetHighCouncilMembersAsyncTest_Exception()
    {
        //Grain Exception
        var result = await _electionService.GetHighCouncilMembersAsync(new HighCouncilMembersInput
        {
            ChainId = ChainIdAELF,
            DaoId = "ThrowException"
        });
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }
}