using System.Threading.Tasks;
using TomorrowDAOServer.Statistic.Dto;

namespace TomorrowDAOServer.Statistic;

public interface IStatisticService
{
    Task<DauDto> GetDauAsync(GetDauInput input);
}