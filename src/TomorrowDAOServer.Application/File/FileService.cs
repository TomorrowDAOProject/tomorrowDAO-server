using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Aws;
using TomorrowDAOServer.File.Provider;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace TomorrowDAOServer.File;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class FileService : TomorrowDAOServerAppService, IFileService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private readonly IAwsS3Client _awsS3Client;
    private readonly IUserProvider _userProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FileService> _logger;

    public FileService(IAwsS3Client awsS3Client, IUserProvider userProvider, IUserBalanceProvider userBalanceProvider, 
        IHttpClientFactory httpClientFactory, ILogger<FileService> logger)
    {
        _awsS3Client = awsS3Client;
        _userBalanceProvider = userBalanceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _userProvider = userProvider;
    }

    public async Task<string> UploadAsync(string chainId, IFormFile file)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var symbol = CommonConstant.GetVotigramSymbol(chainId);
        var userBalance = await _userBalanceProvider.GetByIdAsync(GuidHelper.GenerateGrainId(address, chainId, symbol));
        if (userBalance == null || userBalance.Amount < 1)
        {
            throw new UserFriendlyException("Nft Not enough.");
        }
        if (file == null || file.Length == 0)
        {
            throw new UserFriendlyException("File is null or empty.");
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new UserFriendlyException("File type is not allowed.");
        }

        await using var stream = file.OpenReadStream();
        var utf8Bytes = await stream.GetAllBytesAsync();
        var url = await _awsS3Client.UpLoadFileAsync(new MemoryStream(utf8Bytes), 
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + CommonConstant.Underline + file.FileName);
        return url;
    }

    public async Task<string> UploadFrontEndAsync(string url, string fileName)
    {
        var (baseUrl, extension) = ParseUrl(url);
        await using var stream = await DownloadImageAsync(baseUrl);
        if (stream == null)
        {
            _logger.LogInformation("UploadFrontEndAsyncDownloadFail url {0}", url);
            return string.Empty;
        }

        var memoryStream = await ConvertToWebp(extension, stream);
        if (memoryStream == null)
        {
            _logger.LogInformation("UploadFrontEndAsyncConvertFail url {0}", url);
            return string.Empty;
        }

        var result =  await _awsS3Client.UpLoadFileFrontEndAsync(memoryStream, fileName);
        _logger.LogInformation("UploadFrontEndAsyncResult url {0} result {result}", url, result);
        return result;
    }

    public async Task<Stream> DownloadImageAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DownloadImageAsyncException imageUrl {0}", url);
            return null;
        }
    }

    private async Task<MemoryStream> ConvertToWebp(string extension, Stream stream)
    {
        try
        {
            var memoryStream = new MemoryStream();
            if (extension == ".webp")
            {
                await stream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                using var image = await Image.LoadAsync(stream);
                await image.SaveAsWebpAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
            }
            return memoryStream;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private Tuple<string, string> ParseUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var baseUri = new UriBuilder(uri) { Query = string.Empty }.Uri;
            var extension = Path.GetExtension(baseUri.LocalPath).ToLower();
            return new Tuple<string, string>(baseUri.ToString(), extension);
        }
        catch (Exception)
        {
            _logger.LogInformation("UploadFrontEndAsyncParseUrlFail url {0}", url);
            return new Tuple<string, string>(string.Empty, string.Empty);
        }
    }
}