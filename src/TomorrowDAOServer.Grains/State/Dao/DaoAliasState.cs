namespace TomorrowDAOServer.Grains.State.Dao;

[GenerateSerializer]
public class DaoAliasState
{
    [Id(0)] public List<DaoAlias> DaoList { get; set; }
}

[GenerateSerializer]
public class DaoAlias
{
    [Id(0)] public string DaoId { get; set; }
    [Id(1)] public string DaoName { get; set; }
    [Id(2)] public string CharReplacements { get; set; }
    [Id(3)] public string FilteredChars { get; set; }
    [Id(4)] public int Serial { get; set; }
    [Id(5)] public DateTime CreateTime { get; set; }
}