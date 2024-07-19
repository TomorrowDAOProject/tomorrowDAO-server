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
    }
}