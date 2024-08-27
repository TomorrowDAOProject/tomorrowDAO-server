using System;
using System.Collections.Generic;
using AElf;
using AElf.Types;
using AutoMapper;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer;

public class MapperBase : Profile
{
    protected static Dictionary<string, object> MapTransactionParams(string param)
    {
        return new Dictionary<string, object>
        {
            ["param"] = string.IsNullOrEmpty(param) ? "" : param
        };
    }

    protected static GovernanceMechanism MapGovernanceMechanism(string governanceToken)
    {
        return string.IsNullOrEmpty(governanceToken)
            ? GovernanceMechanism.Organization
            : GovernanceMechanism.Referendum;
    }

    protected static long MapAmount(string amount, int decimals)
    {
        return (long)(amount.SafeToDouble() * Math.Pow(10, decimals));
    }

    protected static string MapAddress(Address address)
    {
        return address?.ToBase58() ?? string.Empty;
    }
    
    protected static string MapChainIdToBase58(int chainId)
    {
        return ChainHelper.ConvertChainIdToBase58(chainId);
    }
}