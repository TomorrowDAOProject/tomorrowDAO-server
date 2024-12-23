using System.Collections.Generic;

namespace TomorrowDAOServer.Common;

public class HolderInfoIndexerDto
{
    public List<HolderInfoDto> CaHolderInfo { get; set; }
}

public class HolderInfoDto
{
    public string OriginChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public string ChainId { get; set; }
    public List<ManagerInfo> ManagerInfos { get; set; }
}

public class ManagerInfo
{
    public string Address { get; set; }
    public string ExtraData { get; set; }
}