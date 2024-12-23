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

    protected static string MapIcon(SourceType sourceType, string icon)
    {
        if (sourceType != SourceType.FindMini)
        {
            return icon;
        }

        if (icon == null)
        {
            return string.Empty;
        }

        return icon.StartsWith("/") ? CommonConstant.FindminiUrlPrefix + icon : icon;
    }
    
    protected static List<string> MapScreenshots(SourceType sourceType, List<string> screenshots)
    {
        if (sourceType != SourceType.FindMini)
        {
            return screenshots;
        }
        
        var res = new List<string>();
        if (screenshots == null || screenshots.IsNullOrEmpty())
        {
            return res;
        }

        res.AddRange(screenshots.Select(screenshot => screenshot.StartsWith("/") ? CommonConstant.FindminiUrlPrefix + screenshot : screenshot));
        
        return res;
    }
}