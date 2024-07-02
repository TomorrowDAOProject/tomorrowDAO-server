using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Election;
using TomorrowDAOServer.Enums;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.DAO.Provider;

public partial class DaoAliasProviderTest : TomorrowDaoServerApplicationTestBase
{
    private readonly IDaoAliasProvider _daoAliasProvider;
    
    public DaoAliasProviderTest(ITestOutputHelper output) : base(output)
    {
        _daoAliasProvider = Application.ServiceProvider.GetRequiredService<IDaoAliasProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockDaoAliasOptions());
        services.AddSingleton(MockGraphQlProvider());
    }

    [Fact]
    public async Task GenerateDaoAliasAsync_Test()
    {
        var daoIndex = new DAOIndex
        {
            Id = "DaoId",
            ChainId = ChainIdAELF,
            Metadata = new Metadata
            {
                Name = "Network Dao"
            }
        };
        var alias = await _daoAliasProvider.GenerateDaoAliasAsync(daoIndex);
        alias.ShouldNotBeNull();
        alias.ShouldBe("network-dao");

        daoIndex.Metadata.Name = "network  Dao";
        alias = await _daoAliasProvider.GenerateDaoAliasAsync(daoIndex);
        alias.ShouldNotBeNull();
        alias.ShouldBe("network-dao");
        
        daoIndex.Metadata.Name = "net??wo####rk  D??#ao&";
        alias = await _daoAliasProvider.GenerateDaoAliasAsync(daoIndex);
        alias.ShouldNotBeNull();
        alias.ShouldBe("network-daoand");
        
        daoIndex.Id = "DaoId.Serial";
        alias = await _daoAliasProvider.GenerateDaoAliasAsync(daoIndex);
        alias.ShouldNotBeNull();
        alias.ShouldBe("network-daoand1");
        
        daoIndex.Id = "DaoId.Exception";
        alias = await _daoAliasProvider.GenerateDaoAliasAsync(daoIndex);
        alias.ShouldNotBeNull();
        alias.ShouldBe("DaoId.Exception");
    }

}