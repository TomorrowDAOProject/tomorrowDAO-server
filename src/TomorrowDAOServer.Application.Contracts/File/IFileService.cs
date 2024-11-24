using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TomorrowDAOServer.File;

public interface IFileService
{
    Task<string> UploadAsync(string chainId, IFormFile file);
    Task<string> UploadAsync(string url, string fileName);
    Task<Stream> DownloadImageAsync(string url);
}