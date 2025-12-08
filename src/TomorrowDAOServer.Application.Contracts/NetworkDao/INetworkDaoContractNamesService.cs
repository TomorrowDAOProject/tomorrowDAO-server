using System.Threading.Tasks;
using TomorrowDAOServer.NetworkDao.Dtos;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoContractNamesService
{
    Task<AddContractNameResponse> AddContractNamesAsync(AddContractNameInput input);
    Task<UpdateContractNameResponse> UpdateContractNamesAsync(UpdateContractNameInput input);
    Task<int> LoadContractHistoryDataAsync(LoadContractHistoryInput input);
    Task<CheckContractNameResponse> CheckContractNameAsync(CheckContractNameInput input);
}