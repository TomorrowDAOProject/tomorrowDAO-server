namespace TomorrowDAOServer.Grains.State.Dao;

public class DaoAliasState
{
    public List<DaoAlias> DaoList { get; set; }
}

public class DaoAlias
{
    public string DaoId { get; set; }
    public string DaoName { get; set; }
    public string CharReplacements { get; set; }
    public string FilteredChars { get; set; }
    public int Serial { get; set; }
    public DateTime CreateTime { get; set; }
}