using System.Threading.Tasks;
using TomorrowDAOServer.Forum.Dto;

namespace TomorrowDAOServer.Forum;

public interface IForumService
{
    Task<LinkPreviewDto> LinkPreviewAsync(LinkPreviewInput input);
}