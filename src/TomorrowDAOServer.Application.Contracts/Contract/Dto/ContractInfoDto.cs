using System.Collections.Generic;

namespace TomorrowDAOServer.Contract.Dto;

public class ContractInfoDto
{
    public string ContractAddress { get; set; }
    public string ContractName { get; set; }
    public List<string> FunctionList { get; set; }
    public List<string> MultiSigDaoMethodBlacklist { get; set; }
}