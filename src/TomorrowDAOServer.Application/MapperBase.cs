using System;
using System.Collections.Generic;
using System.Linq;
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
    protected static ISet<string> UserCreatedMethods = new HashSet<string>() { "CreateProposal", "Release" };

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
    
    protected static List<string> MapCategories(List<TelegramAppCategory> list)
    {
        if (list == null || list.IsNullOrEmpty())
        {
            return new List<string>();
        }

        return list.Select(x => x.ToString()).ToList();
    }
    
    protected static NetworkDaoCreatedByEnum MapCreateBy(string methodName)
    {
        return UserCreatedMethods.Contains(methodName)
            ? NetworkDaoCreatedByEnum.USER
            : NetworkDaoCreatedByEnum.SYSTEM_CONTRACT;
    }

    protected static NetworkDaoProposalStatusEnum MapNetworkDaoProposalStatus(DateTime? expiredTime, NetworkDaoProposalStatusEnum status)
    {
        if (expiredTime == null)
        {
            return status;
        }

        if (status is not (NetworkDaoProposalStatusEnum.Approved or NetworkDaoProposalStatusEnum.Pending))
        {
            return status;
        }

        var now = DateTime.UtcNow;
        return now > expiredTime ? NetworkDaoProposalStatusEnum.Expired : status;

    }
}