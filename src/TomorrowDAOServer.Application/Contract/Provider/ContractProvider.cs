using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Contract.Provider;

public interface IContractProvider
{
    List<ContractInfo> GetContractInfo(string chainId, string contractAddress);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<ContractInfoOptions> _contractInfoOptionsMonitor;

    public ContractProvider(IOptionsMonitor<ContractInfoOptions> contractInfoOptions)
    {
        _contractInfoOptionsMonitor = contractInfoOptions;
    }

    public List<ContractInfo> GetContractInfo(string chainId, string contractAddress)
    {
        if (!_contractInfoOptionsMonitor.CurrentValue.ContractInfos.TryGetValue(chainId, out var info))
        {
            return new List<ContractInfo>();
        }

        if (contractAddress.IsNullOrEmpty())
        {
            return info.Values.ToList();
        }

        return info.TryGetValue(contractAddress, out var contractInfo) ? new List<ContractInfo> { contractInfo } : new List<ContractInfo>();
    }
}