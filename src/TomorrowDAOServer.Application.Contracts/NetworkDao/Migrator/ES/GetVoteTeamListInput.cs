using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetVoteTeamListInput : PagedAndSortedResultRequestDto
{
    public string ChainId { get; set; }
    public string PublicKey { get; set; }
    public bool? IsActive { get; set; }
}