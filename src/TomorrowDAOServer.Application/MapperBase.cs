using System.Collections.Generic;
using AutoMapper;
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

    protected static GovernanceMechanism MapGovernanceMechanism( string governanceToken)
    {
        return string.IsNullOrEmpty(governanceToken) ? GovernanceMechanism.Organization : GovernanceMechanism.Referendum;
    }
}