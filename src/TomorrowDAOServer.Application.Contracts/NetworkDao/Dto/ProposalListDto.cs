
namespace TomorrowDAOServer.NetworkDao.Dto;

public class ProposalListRequest
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Search { get; set; }
    public int IsContract { get; set; }
    public int PageSize { get; set; }
    public int PageNum { get; set; }
    public string Status { get; set; }
    public string ProposalType { get; set; }
}