using System.Collections.Generic;

namespace TomorrowDAOServer.EntityEventHandler.Core.Background.Options;

public class TmrwdaoOption
{
    public bool IsReleaseAuto { get; set; }
    public List<TmrwdaoInfo> TmrwdaoInfoList { get; set; }
    public int CheckTransactionInterval { get; set; } = 30;

}

public class TmrwdaoInfo
{
    public string ChainName { get; set; }
    public string AdminKey { get; set; }
}