using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.File.Provider;

public class FileUploadProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IFileUploadProvider _fileUploadProvider;
    
    public FileUploadProviderTest(ITestOutputHelper output) : base(output)
    {
        _fileUploadProvider = Application.ServiceProvider.GetRequiredService<IFileUploadProvider>();
    }

    [Fact]
    public async Task AddOrUpdateAsyncTest()
    {
        await _fileUploadProvider.AddOrUpdateAsync(ChainIdAELF, Address1, "Url");
    }
}