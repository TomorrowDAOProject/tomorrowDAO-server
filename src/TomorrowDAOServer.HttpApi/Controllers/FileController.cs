using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.File;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("File")]
[Route("api/app/file")]
public class FileController
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost]
    [Authorize]
    [Route("upload")]
    public async Task<string> UploadAsync([Required] string chainId, [Required] IFormFile file)
    {
        return await _fileService.UploadAsync(chainId, file);
    }
}