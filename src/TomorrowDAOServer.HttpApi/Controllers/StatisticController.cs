using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Statistic;
using TomorrowDAOServer.Statistic.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Statistic")]
[Route("api/app/statistic")]
public class StatisticController
{
    private readonly IStatisticService _statisticService;

    public StatisticController(IStatisticService statisticService)
    {
        _statisticService = statisticService;
    }

    [HttpGet("dau")]
    public async Task<DauDto> DauAsync(GetDauInput input)
    {
        return await _statisticService.GetDauAsync(input);
    }
}