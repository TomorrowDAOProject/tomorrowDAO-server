using System;

namespace TomorrowDAOServer.DAO.Indexer;

public class MemberDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public string DAOId { get; set; }
    public string Address { get; set; }
    public DateTime CreateTime { get; set; }
}