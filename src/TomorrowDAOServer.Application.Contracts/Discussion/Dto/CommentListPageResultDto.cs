using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Discussion.Dto;

public class CommentListPageResultDto : PagedResultDto<CommentDto>
{
    public bool HasMore { get; set; }
}