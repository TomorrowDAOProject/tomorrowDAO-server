using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class ChainOptions
{

    public string PrivateKeyForCallTx { get; set; }
    public Dictionary<string, ChainInfo> ChainInfos { get; set; } = new();
    public int TokenImageRefreshDelaySeconds { get; set; } = 300;
    
    public class ChainInfo
    {
        public string BaseUrl { get; set; }
        public bool IsMainChain { get; set; }
        public Dictionary<string, string> ContractAddress { get; set; } = new();
    }
}
