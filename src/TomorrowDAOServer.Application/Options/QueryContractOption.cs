using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class QueryContractOption
{
    public List<QueryContractInfo> QueryContractInfoList { get; set; }

}

public class QueryContractInfo
{
    public string ChainId { get; set; }
    public string PrivateKey { get; set; }
    public string ConsensusContractAddress { get; set; }
    public string ElectionContractAddress { get; set; }
}