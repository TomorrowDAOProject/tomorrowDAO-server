using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Proposal.Dto;

public class ProposalPagedResultDto<T> : PagedResultDto<T>
{
    public PageInfo PreviousPageInfo { get; set; }
    public PageInfo NextPageInfo { get; set; }
}