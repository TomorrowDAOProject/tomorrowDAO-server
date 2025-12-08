using System.Threading.Tasks;
using TomorrowDAOServer.Discussion.Dto;

namespace TomorrowDAOServer.Discussion;

public interface IDiscussionService
{
    Task<NewCommentResultDto> NewCommentAsync(NewCommentInput input);
    Task<CommentListPageResultDto> GetCommentListAsync(GetCommentListInput input);
    Task<CommentBuildingDto> GetCommentBuildingAsync(GetCommentBuildingInput input);
}