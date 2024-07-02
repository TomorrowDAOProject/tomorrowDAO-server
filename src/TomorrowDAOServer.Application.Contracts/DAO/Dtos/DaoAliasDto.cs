using System;

namespace TomorrowDAOServer.DAO.Dtos;

public class DaoAliasDto
{
    public string DaoId { get; set; }
    public string DaoName { get; set; }
    public string Alias { get; set; }
    public string CharReplacements { get; set; }
    public string FilteredChars { get; set; }
    public int Serial { get; set; }
    public DateTime CreateTime { get; set; }
}