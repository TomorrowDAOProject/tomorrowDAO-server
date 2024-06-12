using System.Collections.Generic;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.DAO.Dtos;

public class MyDAOListDto
{
    public MyDAOType Type { get; set; }
    public List<DAOListDto> List { get; set; } = new();
    public long TotalCount { get; set; }
}