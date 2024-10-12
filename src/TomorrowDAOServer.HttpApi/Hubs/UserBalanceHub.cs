using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Volo.Abp.AspNetCore.SignalR;

namespace TomorrowDAOServer.Hubs;

public class UserBalanceHub : AbpHub
{
    private readonly ILogger<UserBalanceHub> _logger;
    private readonly IOptionsMonitor<HubCommonOptions> _hubCommonOptions;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private static readonly ConcurrentDictionary<string, bool> IsPushRunning = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionAddressMap = new();
    private ConcurrentDictionary<string, long> _balanceCache = new();

    public UserBalanceHub(ILogger<UserBalanceHub> logger, IOptionsMonitor<HubCommonOptions> hubCommonOptions, 
        IUserBalanceProvider userBalanceProvider)
    {
        _logger = logger;
        _hubCommonOptions = hubCommonOptions;
        _userBalanceProvider = userBalanceProvider;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        ConnectionAddressMap.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }
    
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