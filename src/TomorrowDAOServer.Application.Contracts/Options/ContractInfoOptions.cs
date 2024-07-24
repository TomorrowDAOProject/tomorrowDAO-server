using System.Collections.Generic;

namespace TomorrowDAOServer.Options;

public class ContractInfoOptions
{
    public Dictionary<string, Dictionary<string, ContractInfo>> ContractInfos { get; set; }
}

public class ContractInfo
{
    public string ContractAddress { get; set; }
    public string ContractName { get; set; }
    public List<string> FunctionList { get; set; }
    public List<string> MultiSigDaoMethodBlacklist { get; set; }
}