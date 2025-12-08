using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.File;

public partial class FileServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IFileService _fileService;

    public FileServiceTest(ITestOutputHelper output) : base(output)
    {
        _fileService = Application.ServiceProvider.GetRequiredService<IFileService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockAwsS3Client());
    }

    [Fact]
    public async Task UploadAsyncTest()
    {
        Login(Guid.NewGuid());
        // var uploadAsync = await _fileService.UploadAsync(ChainIdAELF,
        //     new FormFile(new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 }), 0, 4, "name", "fileName"));
        // var uploadAsync = await _fileService.UploadAsync(ChainIdAELF,
        //     MockFormFile());
        // uploadAsync.ShouldNotBeNull();
    }
}