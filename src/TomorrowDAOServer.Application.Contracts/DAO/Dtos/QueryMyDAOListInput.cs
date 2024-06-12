using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.DAO.Dtos;

public class QueryMyDAOListInput : QueryDAOListInput
{
    public MyDAOType Type { get; set; }
    public string Address { get; set; }
}