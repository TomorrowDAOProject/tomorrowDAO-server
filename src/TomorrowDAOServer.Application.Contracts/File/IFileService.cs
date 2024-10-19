using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TomorrowDAOServer.File;

public interface IFileService
{
    Task<string> UploadAsync(string chainId, IFormFile file);
}