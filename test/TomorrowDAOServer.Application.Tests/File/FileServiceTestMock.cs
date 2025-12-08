using System.IO;
using Microsoft.AspNetCore.Http;
using Moq;
using TomorrowDAOServer.Common.Aws;

namespace TomorrowDAOServer.File;

public partial class FileServiceTest
{
    private IFormFile MockFormFile()
    {
        var mock = new Mock<IFormFile>();

        mock.Setup(m => m.Length).Returns(1000);

        mock.Setup(m => m.ContentDisposition).Returns("http://23e4.io");
        
        return mock.Object;
    }

    private IAwsS3Client MockAwsS3Client()
    {
        var mock = new Mock<IAwsS3Client>();

        mock.Setup(m => m.UpLoadFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync((Stream steam, string fileName) => { return "http://image.url.io"; });

        return mock.Object;
    }
}