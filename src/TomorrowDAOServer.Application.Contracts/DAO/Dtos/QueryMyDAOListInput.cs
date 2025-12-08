using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.DAO.Dtos;

public class QueryMyDAOListInput : QueryPageInput
{
    public MyDAOType Type { get; set; }
}