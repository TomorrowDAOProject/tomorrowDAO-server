using System.Threading.Tasks;
using TomorrowDAOServer.Spider.Dto;

namespace TomorrowDAOServer.Spider;

public interface IForumSpiderService
{
    Task<LinkPreviewDto> LinkPreviewAsync(LinkPreviewInput input);
}