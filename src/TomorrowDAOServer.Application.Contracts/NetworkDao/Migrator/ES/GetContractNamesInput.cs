namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetContractNamesInput
{
    public string ChainId { get; set; }
    public string ContractName { get; set; }
    public string ContractAddress { get; set; }
}