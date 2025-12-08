namespace TomorrowDAOServer.DAO.Indexer;

public class IndexerDAOVoterRecord
{
    public string Id { get; set; }
    public string DaoId { get; set; }
    public string VoterAddress { get; set; }
    public int Count { get; set; }
    public long Amount { get; set; }
    public string ChainId { get; set; }
}