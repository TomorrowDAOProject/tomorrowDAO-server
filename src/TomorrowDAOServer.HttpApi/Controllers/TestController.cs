using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Ranking.Provider;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Ranking")]
[Route("api/app/test")]
public class TestController
{
    private IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;

    public TestController(IRankingAppPointsRedisProvider rankingAppPointsRedisProvider)
    {
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
    }
    
    [HttpGet("redis-value")]
    public async Task<string> GetRedisValueAsync(string key)
    {
        return await _rankingAppPointsRedisProvider.GetAsync(key);
    }
}