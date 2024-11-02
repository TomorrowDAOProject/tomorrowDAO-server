using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Contract.Dto;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Contract;

public partial class ContractServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IContractService _contractService;
    
    public ContractServiceTest(ITestOutputHelper output) : base(output)
    {
        _contractService = Application.ServiceProvider.GetRequiredService<IContractService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockDaoProvider());
    }

    [Fact]
    public async Task GetContractInfo_Test()
    {
        var result = await _contractService.GetContractInfoAsync(new QueryContractsInfoInput
        {
            ChainId = ChainIdAELF,
            DaoId = "DaoId",
            GovernanceMechanism = GovernanceMechanism.Organization
        });
        result.ShouldNotBeNull();
        result.ContractInfoList.ShouldBeEmpty();
        
        result = await _contractService.GetContractInfoAsync(new QueryContractsInfoInput
        {
            ChainId = ChainIdtDVW,
            DaoId = "DaoId",
            GovernanceMechanism = null
        });
        result.ShouldNotBeNull();
        result.ContractInfoList.ShouldNotBeNull();
        result.ContractInfoList.Count.ShouldBe(2);
    }

    [Fact]
    public void GetFunctionList_Test()
    {
        var result = _contractService.GetFunctionList(ChainIdAELF, "contractAddress");
        result.ShouldNotBeNull();
    }
}