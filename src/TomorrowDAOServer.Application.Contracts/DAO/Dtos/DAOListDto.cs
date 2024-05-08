namespace TomorrowDAOServer.DAO.Dtos;

public class DAOListDto
{
    public string ChainId { get; set; }
    public string DaoId { get; set; }
    public string Logo { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Creator { get; set; }
    public long ProposalsNum { get; set; } = 0;
    public string Symbol { get; set; }
    public long SymbolHoldersNum { get; set; } = 0;
    public long VotersNum { get; set; } = 0;
    public bool IsNetworkDAO { get; set; }
}