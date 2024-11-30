using System.Collections.Generic;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Migrator.GraphQL;

public class GetNetworkDaoDataInput
{
    public string? ChainId { get; set; }
    public List<string>? OrgAddresses { get; set; }
    public NetworkDaoOrgType OrgType { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 1000;
}