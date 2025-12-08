using System.Threading.Tasks;

namespace TomorrowDAOServer.Chains
{
    public interface IChainAppService
    {
        Task<string[]> GetListAsync();
        
        Task<string> GetChainIdAsync(int index);
    }
}