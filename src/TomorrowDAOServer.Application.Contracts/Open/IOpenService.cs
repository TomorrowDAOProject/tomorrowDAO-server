using System.Threading.Tasks;

namespace TomorrowDAOServer.Open;

public interface IOpenService
{
    Task<TaskStatusResponse> GetTaskStatusAsync(string address, string proposalId);
}