using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.DAO.Dtos;

public class QueryDAOListInput : QueryPageInput
{
    public DAOType DaoType { get; set; }
}