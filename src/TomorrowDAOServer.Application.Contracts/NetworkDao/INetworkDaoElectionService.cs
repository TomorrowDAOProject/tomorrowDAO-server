using System.Threading.Tasks;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoElectionService
{
    Task<long> GetBpVotingStakingAmount();
}