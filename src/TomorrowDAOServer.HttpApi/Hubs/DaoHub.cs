using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Volo.Abp.AspNetCore.SignalR;

namespace TomorrowDAOServer.Hubs;

public class DaoHub : AbpHub
{
    private readonly ILogger<DaoHub> _logger;
    private static readonly ConcurrentDictionary<string, bool> IsPushRunning = new();
    private readonly IHubContext<DaoHub> _hubContext;
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IDatabase _redisDatabase;
    private readonly IOptionsMonitor<HubCommonOptions> _hubCommonOptions;
    private static readonly ConcurrentDictionary<string, string> ConnectionAddressMap = new();
    private readonly ConcurrentDictionary<string, long> _balanceCache = new();
    private ConcurrentDictionary<string, List<RankingAppPointsBaseDto>> _pointsCache = new();

    public DaoHub(ILogger<DaoHub> logger, IHubContext<DaoHub> hubContext,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider, IOptionsMonitor<HubCommonOptions> hubCommonOptions,
        IUserBalanceProvider userBalanceProvider, IProposalProvider proposalProvider, IConnectionMultiplexer connectionMultiplexer)
    {
        _logger = logger;
        _hubContext = hubContext;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _hubCommonOptions = hubCommonOptions;
        _userBalanceProvider = userBalanceProvider;
        _proposalProvider = proposalProvider;
        _redisDatabase = connectionMultiplexer.GetDatabase();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        ConnectionAddressMap.TryRemove(Context.ConnectionId, out _);
        await RemoveFromConnectionProposalIdMap(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    
    // -------------------------------------userPoints-------------------------------------
    public async Task UnsubscribePointsProduce(UserPointsRequest input)
    {
        await RemoveFromConnectionProposalIdMap(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubHelper.GetPointsGroupName(input.ChainId, input.ProposalId));
    }

    public async Task RequestPointsProduce(UserPointsRequest input)
    {
        var chainId = input.ChainId;
        var proposalId = input.ProposalId;
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("RequestPointsProduceBegin, chainId {chainId}, proposalId {proposalId}", chainId, proposalId);
        var previousProposalId = await GetProposalIdByConnectionId(connectionId);
        if (!string.IsNullOrEmpty(previousProposalId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubHelper.GetPointsGroupName(input.ChainId, previousProposalId));
        }
        await Groups.AddToGroupAsync(connectionId, HubHelper.GetPointsGroupName(chainId, proposalId));
        var currentPoints = await GetAllAppPointsAsync(chainId, proposalId);
        await Clients.Caller.SendAsync(CommonConstant.RequestPointsProduce, new PointsProduceDto { PointsList = currentPoints });
        _logger.LogInformation("RequestPointsProduceEnd, chainId {chainId}, proposalId {proposalId}", chainId, proposalId);
        await PushRequestPointsProduceAsync(chainId);
    }

    private async Task PushRequestPointsProduceAsync(string chainId)
    {
        var key = HubHelper.GetPointsGroupName(chainId);
        if (!IsPushRunning.TryAdd(key, true))
        {
            _logger.LogInformation("PushRequestPointsProduceAsyncIsRunning, chainId {chainId}", chainId);
            return;
        }

        try
        {
            while (true)
            {
                await Task.Delay(_hubCommonOptions.CurrentValue.GetDelay(key));
                var proposalIds = await GetDistinctProposalIdsAsync();
                var currentPoints = await GetAllAppPointsAsync(chainId, proposalIds);
                var changedPoints = new Dictionary<string, List<RankingAppPointsBaseDto>>();
                foreach (var (proposalId, pointsList) in currentPoints)
                {
                    if (_pointsCache.TryGetValue(proposalId, out var cachedList))
                    {
                        if (!IsEqual(pointsList, cachedList))
                        {
                            changedPoints[proposalId] = pointsList;
                        }
                    }
                    else
                    {
                        changedPoints[proposalId] = pointsList;
                    }
                }
                foreach (var (proposalId, pointsList) in changedPoints)
                {
                    await _hubContext.Clients.Groups(HubHelper.GetPointsGroupName(chainId, proposalId))
                        .SendAsync(CommonConstant.ReceivePointsProduce, new PointsProduceDto { PointsList = pointsList });
                }
                _pointsCache = new ConcurrentDictionary<string, List<RankingAppPointsBaseDto>>(currentPoints);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("PushRequestPointsProduceAsyncException: {e}", e);
        }
        finally
        {
            IsPushRunning.TryRemove(key, out _);
        }
    }

    private async Task<Dictionary<string, List<RankingAppPointsBaseDto>>> GetAllAppPointsAsync(string chainId, List<string> proposalIds)
    {
        var proposalList = await _proposalProvider.GetProposalByIdsAsync(chainId, proposalIds);
        var aliasListDic = proposalList.ToDictionary(
            proposal => proposal.ProposalId,
            proposal => RankHelper.GetAliasList(proposal.ProposalDescription));
        var allAppPointsDict = new Dictionary<string, List<RankingAppPointsBaseDto>>();
        foreach (var proposalId in proposalIds)
        {
            if (aliasListDic.TryGetValue(proposalId, out var aliasList))
            {
                allAppPointsDict[proposalId] = RankingAppPointsDto
                    .ConvertToBaseList(await _rankingAppPointsRedisProvider.GetAllAppPointsAsync(chainId, proposalId, aliasList))
                    .OrderByDescending(x => x.Points).ToList();
            }
        }

        return allAppPointsDict;
    }

    private async Task<List<RankingAppPointsBaseDto>> GetAllAppPointsAsync(string chainId, string proposalId)
    {
        var proposal = await _proposalProvider.GetProposalByIdAsync(chainId, proposalId);
        var aliasList = RankHelper.GetAliasList(proposal.ProposalDescription);
        return RankingAppPointsDto
            .ConvertToBaseList(await _rankingAppPointsRedisProvider.GetAllAppPointsAsync(chainId, proposalId, aliasList))
            .OrderByDescending(x => x.Points).ToList();
    }

    private bool IsEqual(IReadOnlyCollection<RankingAppPointsBaseDto> currentPoints, IReadOnlyCollection<RankingAppPointsBaseDto> cachedPoints)
    {
        return currentPoints.Count == cachedPoints.Count
               && !currentPoints.Except(cachedPoints, new AllFieldsEqualComparer<RankingAppPointsBaseDto>()).Any();
    }
    
    private async Task AddToConnectionProposalIdMap(string connectionId, string proposalId)
    {
        await _redisDatabase.HashSetAsync(CommonConstant.ConnectionProposalIdMap, connectionId, proposalId);
    }

    private async Task RemoveFromConnectionProposalIdMap(string connectionId)
    {
        await _redisDatabase.HashDeleteAsync(CommonConstant.ConnectionProposalIdMap, connectionId);
    }
    
    private async Task<HashEntry[]> GetFromConnectionProposalIdMap()
    {
        return await _redisDatabase.HashGetAllAsync(CommonConstant.ConnectionProposalIdMap);
    }
    
    private async Task<string> GetProposalIdByConnectionId(string connectionId)
    {
        var proposalIdValue = await _redisDatabase.HashGetAsync(CommonConstant.ConnectionProposalIdMap, connectionId);
        return proposalIdValue.HasValue ? proposalIdValue.ToString() : string.Empty;
    }

    private async Task<List<string>> GetDistinctProposalIdsAsync()
    {
        var allProposals = await GetFromConnectionProposalIdMap();
        return allProposals.Select(entry => (string)entry.Value)
            .Distinct()
            .ToList();
    }
    
    // -------------------------------------userBalance-------------------------------------
    public Task UnsubscribeUserBalanceProduce()
    {
        ConnectionAddressMap.TryRemove(Context.ConnectionId, out _);
        return Task.CompletedTask;
    }

    public async Task RequestUserBalanceProduce(UserBalanceRequest input)
    {
        var chainId = input.ChainId;
        var address = input.Address;
        var connectionId = Context.ConnectionId;
        ConnectionAddressMap[connectionId] = address;
        var symbol = CommonConstant.GetVotigramSymbol(input.ChainId);
        var userBalance = await _userBalanceProvider.GetByIdAsync(GuidHelper.GenerateGrainId(address, input.ChainId, symbol));
        var balance = userBalance?.Amount ?? 0;
        _balanceCache[address] = balance;
        await Clients.Caller.SendAsync(CommonConstant.RequestUserBalanceProduce, new UserBalanceProduceDto
        {
            Address = address,
            Symbol = symbol,
            BeforeAmount = -1, 
            NowAmount = balance
        });
        await PushRequestUserBalanceProduceAsync(chainId);
    }

    private async Task PushRequestUserBalanceProduceAsync(string chainId)
    {
        var groupName = HubHelper.GetUserBalanceGroupName(chainId);
        if (!IsPushRunning.TryAdd(groupName, true))
        {
            _logger.LogInformation("PushRequestUserBalanceProduceAsyncIsRunning, chainId {chainId}", chainId);
            return;
        }

        try
        {
            while (true)
            {
                await Task.Delay(_hubCommonOptions.CurrentValue.GetDelay(groupName));
                var symbol = CommonConstant.GetVotigramSymbol(chainId);
                var addressList = ConnectionAddressMap.Values.Distinct().ToList();
                var currentBalanceList = await _userBalanceProvider.GetAllUserBalanceAsync(chainId, symbol, addressList);
                foreach (var balanceIndex in currentBalanceList)
                {
                    var address = balanceIndex.Address;
                    var newBalance = balanceIndex?.Amount ?? 0;
                    if (!_balanceCache.TryGetValue(address, out var cachedBalance))
                    {
                        cachedBalance = -1;
                    }

                    if (newBalance == cachedBalance)
                    {
                        continue;
                    }

                    _balanceCache[address] = newBalance;
                    var connectionIds = ConnectionAddressMap.Where(pair => pair.Value == address)
                        .Select(pair => pair.Key).ToList();
                    foreach (var connectionId in connectionIds)
                    {
                        await Clients.Client(connectionId).SendAsync(CommonConstant.ReceiveUserBalanceProduce, 
                            new UserBalanceProduceDto
                            {
                                Address = address, Symbol = symbol,
                                BeforeAmount = cachedBalance, NowAmount = newBalance 
                            });
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PushRequestUserBalanceProduceAsyncException: chainId {chainId}", chainId);
        }
        finally
        {
            IsPushRunning.TryRemove(groupName, out _);
        }
    }
}