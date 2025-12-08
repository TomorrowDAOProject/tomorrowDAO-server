using System;
using Microsoft.IdentityModel.Tokens;

namespace TomorrowDAOServer.Common;

public static class AddressHelper
{


    public static string ToFullAddress(string chainId, string address)
    {
        var (_, shortAddress) = FromFullAddress(address);
        if (shortAddress.IsNullOrEmpty()) return shortAddress;
        return string.Join(CommonConstant.Underline, CommonConstant.ELF, shortAddress, chainId);
    }

    
    public static Tuple<string, string> FromFullAddress(string fullAddress)
    {
        var vals = fullAddress.Split(CommonConstant.Underline);
        if (vals.IsNullOrEmpty()) return null;
        if (vals.Length == 1) return Tuple.Create(CommonConstant.MainChainId, vals[0]);
        if (vals.Length == 2) return Tuple.Create(CommonConstant.MainChainId, vals[1]);
        return Tuple.Create(vals[2], vals[1]);
    }
    
}