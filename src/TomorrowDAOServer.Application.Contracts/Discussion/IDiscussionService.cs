using System.Threading.Tasks;
using TomorrowDAOServer.Discussion.Dto;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Discussion;

public interface IDiscussionService
{
    Task<bool> NewCommentAsync(NewCommentInput input);
    Task<PagedResultDto<CommentDto>> GetCommentListAsync(GetCommentListInput input);
}