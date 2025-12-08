using System.Collections.Generic;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Options;
using Xunit;

namespace TomorrowDAOServer.Contract.Provider;

public class ContractProviderTest
{ 
    private static readonly IOptionsMonitor<ContractInfoOptions> ContractInfoOptionsMonitor = Substitute.For<IOptionsMonitor<ContractInfoOptions>>();
    private readonly ContractProvider _contractProvider = new(ContractInfoOptionsMonitor);
    
    [Fact]
    public void GetContractInfo_Test()
    {
        ContractInfoOptionsMonitor.CurrentValue.Returns(new ContractInfoOptions
        {
            ContractInfos = new Dictionary<string, Dictionary<string, ContractInfo>>
            {
                ["AELF"] = new()
                {
                    ["DAOContractAddress"] = new ContractInfo
                    {
                        ContractAddress = "DAOContractAddress",
                        ContractName = "DAOContract",
                        FunctionList = new List<string>{"func1", "func2"}
                    }
                }
            }
        });
        
        var result = _contractProvider.GetContractInfo("WRONGCHAIN", "DAOContractAddress");
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
        
        result = _contractProvider.GetContractInfo("AELF", string.Empty);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        CheckContractInfo(result[0]);
        
        result = _contractProvider.GetContractInfo("AELF", "DAOContractAddress");
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        CheckContractInfo(result[0]);
    }

    private static void CheckContractInfo(ContractInfo contractInfo)
    {
        contractInfo.ContractName.ShouldBe("DAOContract");
        contractInfo.ContractAddress.ShouldBe("DAOContractAddress");
        var functionList = contractInfo.FunctionList;
        functionList.Count.ShouldBe(2);
    }
}