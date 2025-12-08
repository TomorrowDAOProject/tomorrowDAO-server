using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.ThirdPart.Exchange;

namespace TomorrowDAOServer.Token;

public class TokenPriceUpdateService : ScheduleSyncDataService
{
    private readonly ILogger<TokenPriceUpdateService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IOptionsMonitor<NetworkDaoOptions> _networkDaoOptions;
    private readonly ITokenService _tokenService;
    
    public TokenPriceUpdateService(ILogger<TokenPriceUpdateService> logger,
        IGraphQLProvider graphQlProvider, IChainAppService chainAppService,
        IOptionsMonitor<NetworkDaoOptions> networkDaoOptions, ITokenService tokenService)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _networkDaoOptions = networkDaoOptions;
        _tokenService = tokenService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var awakenSymbols = _networkDaoOptions.CurrentValue.AwakenSymbols;
        var aetherLinkSymbols = _networkDaoOptions.CurrentValue.AetherLinkSymbols;
        var otherSymbols = _networkDaoOptions.CurrentValue.PopularSymbols;
        var awakenTasks = awakenSymbols.Select(symbol => _tokenService.UpdateExchangePriceAsync(
            symbol.ToUpper(), CommonConstant.USD, new List<ExchangeProviderName> { ExchangeProviderName.Awaken })).ToList();
        var aetherLinkTasks = aetherLinkSymbols.Select(symbol => _tokenService.UpdateExchangePriceAsync(
            symbol.ToUpper(), CommonConstant.USD, new List<ExchangeProviderName> { ExchangeProviderName.AetherLink })).ToList();
        var otherTasks = otherSymbols.Select(symbol => _tokenService.UpdateExchangePriceAsync(
            symbol.ToUpper(), CommonConstant.USD, new List<ExchangeProviderName>())).ToList();
        await Task.WhenAll(awakenTasks.Concat(aetherLinkTasks).Concat(otherTasks));
        return -1;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.TokenPriceUpdate;
    }
}