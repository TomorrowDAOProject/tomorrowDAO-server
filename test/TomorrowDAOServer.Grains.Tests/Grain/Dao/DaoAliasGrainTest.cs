using System.Threading.Tasks;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Grains.Grain.Dao;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.Dao;

[CollectionDefinition(ClusterCollection.Name)]
public class DaoAliasGrainTest : TomorrowDAOServerGrainsTestsBase
{
    private const string Alias = "dao-name";
    public DaoAliasGrainTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task SaveDaoAliasInfoAsyncTest()
    {
        var grainId = GuidHelper.GenerateId(ChainIdAELF, Alias);
        var daoAliasGrain = Cluster.GrainFactory.GetGrain<IDaoAliasGrain>(grainId);

        var resultDto = await daoAliasGrain.SaveDaoAliasInfoAsync(null);
        resultDto.ShouldNotBeNull();
        //resultDto.Success.ShouldBeFalse();
        resultDto.Message.ShouldBe("The parameter is null");
        
        resultDto = await daoAliasGrain.SaveDaoAliasInfoAsync(new DaoAliasDto
        {
            DaoId = DaoId,
            DaoName = "Dao Name",
            Alias = Alias,
            CharReplacements = "",
            FilteredChars = "",
            Serial = 0
        });
        resultDto.ShouldNotBeNull();
        resultDto.Success.ShouldBeTrue();
        

        var grainResultDto = await daoAliasGrain.GetDaoAliasInfoAsync();
        grainResultDto.ShouldNotBeNull();
        grainResultDto.Success.ShouldBeTrue();
        grainResultDto.Data.ShouldNotBeNull();
        grainResultDto.Data[0].Alias.ShouldBeNull();
        grainResultDto.Data[0].DaoId.ShouldBe(DaoId);
    }
}