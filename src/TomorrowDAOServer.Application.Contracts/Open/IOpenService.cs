using System.Threading.Tasks;
using TomorrowDAOServer.Open.Dto;

namespace TomorrowDAOServer.Open;

public interface IOpenService
{
    Task<TaskStatusResponse> GetMicro3TaskStatusAsync(string address);
    Task<bool> GetFoxCoinTaskStatusAsync(string id, string type);
    Task<GetGalxeTaskStatusDto> GetGalxeTaskStatusAsync(GetGalxeTaskStatusInput input);
}