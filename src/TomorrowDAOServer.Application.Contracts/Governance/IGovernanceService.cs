using System.Threading.Tasks;
using TomorrowDAOServer.Governance.Dto;

namespace TomorrowDAOServer.Governance;

public interface IGovernanceService
{
    Task<GovernanceSchemeDto> GetGovernanceSchemeAsync(GetGovernanceSchemeListInput input);
}