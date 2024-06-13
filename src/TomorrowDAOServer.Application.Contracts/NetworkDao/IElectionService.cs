using System.Threading.Tasks;

namespace TomorrowDAOServer.NetworkDao;

public interface IElectionService
{
    Task<long> GetBpVotingStakingAmount();
}