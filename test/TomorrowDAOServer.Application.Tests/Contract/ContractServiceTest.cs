using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Contract.Dto;
using TomorrowDAOServer.Contract.Provider;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace TomorrowDAOServer.Contract;

public class ContractServiceTest
{
    private static readonly IContractProvider ContractProvider = Substitute.For<IContractProvider>();

    private readonly ContractService _contractService = new(Substitute.For<IObjectMapper>(), ContractProvider,
        Substitute.For<IDAOProvider>(),
        Substitute.For<ILogger<ContractService>>());

    [Fact]
    public void GetContractInfo_Test()
    {
        var result = _contractService.GetContractInfoAsync(new QueryContractsInfoInput
        {
            ChainId = "AELF",
            DaoId = "DaoId",
            GovernanceMechanism = GovernanceMechanism.Organization
        });
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetFunctionList_Test()
    {
        ContractProvider.GetContractInfo(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new List<ContractInfo>());

        var result = _contractService.GetFunctionList("AELF", "contractAddress");
        result.ShouldNotBeNull();
    }
}