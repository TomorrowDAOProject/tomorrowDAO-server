using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Common.Aws;

public interface IAwsS3Client
{
    Task<string> UpLoadFileAsync(Stream steam, string fileName);
    Task<string> UpLoadBase64FileAsync(string base64Image, string fileName);
}

public class AwsS3Client : IAwsS3Client, ITransientDependency
{
    private readonly ILogger<AwsS3Client> _logger;
    private readonly IOptionsMonitor<AwsS3Option> _awsS3Option;
    private readonly IOptionsMonitor<SecurityServerOptions> _securityServerOption;
    private readonly ISecretProvider _secretProvider;
    private AmazonS3Client _amazonS3Client;

    public AwsS3Client(IOptionsMonitor<AwsS3Option> awsS3Option, ISecretProvider secretProvider,
        ILogger<AwsS3Client> logger, IOptionsMonitor<SecurityServerOptions> securityServerOption)
    {
        _awsS3Option = awsS3Option;
        _secretProvider = secretProvider;
        _logger = logger;
        _securityServerOption = securityServerOption;
        // InitAmazonS3Client();
    }

    private void InitAmazonS3Client()
    {
        try
        {
            var identityPoolId = AsyncHelper.RunSync(() =>
                _secretProvider.GetSecretWithCacheAsync(_securityServerOption.CurrentValue.KeyIds.AwsS3IdentityPool));
            var cognitoCredentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.APNortheast1);
            _amazonS3Client = new AmazonS3Client(cognitoCredentials, RegionEndpoint.APNortheast1);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Init AwsS3 client ERROR");
        }
    }



    public async Task<string> UpLoadBase64FileAsync(string base64Image, string fileName)
    {
        // base64Image = base64Image.TrimStart("data:image/png;base64".ToCharArray());
        var vals = base64Image.Split(CommonConstant.Comma);
        AssertHelper.NotEmpty(vals, "Invalid image base64");
        var imageBytes = Convert.FromBase64String(vals[vals.Length - 1]);
        using var ms = new MemoryStream(imageBytes);
        return await UpLoadFileAsync(ms, fileName);
    }

    public async Task<string> UpLoadFileAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.CurrentValue.BucketName,
            Key = _awsS3Option.CurrentValue.Path + "/images/token/" + fileName,
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK
            ? $"https://{_awsS3Option.CurrentValue.BucketName}.s3.amazonaws.com/{putObjectRequest.Key}"
            : string.Empty;
    }
}