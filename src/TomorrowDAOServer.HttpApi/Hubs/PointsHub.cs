using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using Volo.Abp.AspNetCore.SignalR;

namespace TomorrowDAOServer.Hubs;

public class PointsHub : AbpHub
{
    private readonly ILogger<PointsHub> _logger;
    private static readonly ConcurrentDictionary<string, bool> IsPushRunning = new();
    private readonly IHubContext<PointsHub> _hubContext;
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IOptionsMonitor<HubCommonOptions> _hubCommonOptions;
    private List<RankingAppPointsBaseDto> _pointsCache = new();

    public PointsHub(ILogger<PointsHub> logger, IHubContext<PointsHub> hubContext,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IOptionsMonitor<HubCommonOptions> hubCommonOptions)
    {
        _logger = logger;
        _hubContext = hubContext;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _hubCommonOptions = hubCommonOptions;
    }
    
    public async Task UnsubscribePointsProduce(string chainId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubHelper.GetPointsGroupName(chainId));
    }

    public async Task RequestPointsProduce(string chainId)
    {
        _logger.LogInformation("RequestPointsProduceBegin, chainId {chainId}", chainId);
        await Groups.AddToGroupAsync(Context.ConnectionId, HubHelper.GetPointsGroupName(chainId));
        var currentPoints = await GetDefaultAllAppPointsAsync(chainId);
        await Clients.Caller.SendAsync(CommonConstant.ReceivePointsProduce, currentPoints);
        _logger.LogInformation("RequestPointsProduceEnd, chainId {chainId}", chainId);
        await PushRequestBpProduceAsync(chainId);
    }

    private async Task PushRequestBpProduceAsync(string chainId)
    {
        var key = HubHelper.GetPointsGroupName(chainId);
        if (!IsPushRunning.TryAdd(key, true))
        {
            return;
        }

        try
        {
            while (true)
            {
                await Task.Delay(_hubCommonOptions.CurrentValue.GetDelay(key));
                _logger.LogInformation("PushRequestBpProduceAsyncBegin, chainId {chainId}", chainId);
                var currentPoints = await GetDefaultAllAppPointsAsync(chainId);
                if (IsEqual(currentPoints))
                {
                    _logger.LogInformation("PushRequestBpProduceAsyncNoNeedToPush, chainId {chainId}", chainId);
                }
                else
                {
                    _logger.LogInformation("PushRequestBpProduceAsyncNeedToPush, chainId {chainId}", chainId);
                    await _hubContext.Clients.Groups(HubHelper.GetPointsGroupName(chainId))
                        .SendAsync(CommonConstant.ReceivePointsProduce, currentPoints);
                }
                _pointsCache = currentPoints;
                _logger.LogInformation("PushRequestBpProduceAsyncEnd, chainId {chainId}", chainId);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("PushRequestBpProduceAsyncException: {e}", e);
        }
        finally
        {
            IsPushRunning.TryRemove(key, out _);
        }
    }

    private async Task<List<RankingAppPointsBaseDto>> GetDefaultAllAppPointsAsync(string chainId)
    {
        return RankingAppPointsDto
            .ConvertToBaseList(await _rankingAppPointsRedisProvider.GetDefaultAllAppPointsAsync(chainId))
            .OrderByDescending(x => x.Points).ToList();
    }

    private bool IsEqual(IReadOnlyCollection<RankingAppPointsBaseDto> currentPoints)
    {
        return currentPoints.Count == _pointsCache.Count
               && !currentPoints.Except(_pointsCache, new AllFieldsEqualComparer<RankingAppPointsBaseDto>()).Any();
    }
}