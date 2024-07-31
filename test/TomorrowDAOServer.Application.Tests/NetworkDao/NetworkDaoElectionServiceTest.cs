using System.Reflection;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TomorrowDAOServer.NetworkDao;

public partial class NetworkDaoTest
{
    [Fact]
    public async Task GetBpVotingStakingAmountTest()
    {
        var stakingAmount = await _networkDaoElectionService.GetBpVotingStakingAmount();
        stakingAmount.ShouldBe(20);


        var fieldInfo = typeof(NetworkDaoElectionService).GetField("_lastQueryAmount", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        fieldInfo.ShouldNotBeNull();
        
        fieldInfo.SetValue(_networkDaoElectionService, 21);
        fieldInfo.GetValue(_networkDaoElectionService).ShouldBe(21);

        //query cache
        stakingAmount = await _networkDaoElectionService.GetBpVotingStakingAmount();
        stakingAmount.ShouldBe(21);
    }
}