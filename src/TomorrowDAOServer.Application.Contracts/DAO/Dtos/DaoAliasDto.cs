using System;
using Orleans;

namespace TomorrowDAOServer.DAO.Dtos;

[GenerateSerializer]
public class DaoAliasDto
{
    [Id(0)] public string DaoId { get; set; }
    [Id(1)] public string DaoName { get; set; }
    [Id(2)] public string Alias { get; set; }
    [Id(3)] public string CharReplacements { get; set; }
    [Id(4)] public string FilteredChars { get; set; }
    [Id(5)] public int Serial { get; set; }
    [Id(6)] public DateTime CreateTime { get; set; }
}