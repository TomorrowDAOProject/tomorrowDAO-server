using System.Threading.Tasks;
using TomorrowDAOServer.Open.Dto;

namespace TomorrowDAOServer.Open;

public interface IOpenService
{
    Task<TaskStatusResponse> GetTaskStatusAsync(string address, string proposalId);
}