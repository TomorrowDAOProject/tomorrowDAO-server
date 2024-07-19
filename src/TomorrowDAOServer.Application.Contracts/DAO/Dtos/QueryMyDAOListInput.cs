using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.DAO.Dtos;

public class QueryMyDAOListInput : QueryDAOListInput
{
    public MyDAOType Type { get; set; }
}