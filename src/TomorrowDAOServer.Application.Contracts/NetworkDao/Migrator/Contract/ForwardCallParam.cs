namespace TomorrowDAOServer.NetworkDao.Migrator.Contract;

public class ForwardCallParam
{
    public string CaHash { get; set; }
    public string MethodName { get; set; } 
    public string Args { get; set; }
    public string ContractAddress { get; set; }
}