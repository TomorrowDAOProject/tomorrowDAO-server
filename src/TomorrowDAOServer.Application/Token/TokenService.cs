using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Token.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.ThirdPart.Exchange;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Token;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TokenService : TomorrowDAOServerAppService, ITokenService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TokenService> _logger;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly Dictionary<string, IExchangeProvider> _exchangeProviders;
    private readonly IOptionsMonitor<NetWorkReflectionOptions> _netWorkReflectionOption;
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    
    public TokenService(IClusterClient clusterClient, ILogger<TokenService> logger, IExplorerProvider explorerProvider, 
        IObjectMapper objectMapper, IGraphQLProvider graphQlProvider, IEnumerable<IExchangeProvider> exchangeProviders,
        IOptionsMonitor<NetWorkReflectionOptions> netWorkReflectionOption, IOptionsMonitor<ExchangeOptions> exchangeOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _explorerProvider = explorerProvider;
        _objectMapper = objectMapper;
        _graphQlProvider = graphQlProvider;
        _exchangeProviders = exchangeProviders.ToDictionary(p => p.Name().ToString());
        _netWorkReflectionOption = netWorkReflectionOption;
        _exchangeOptions = exchangeOptions;
    }

    public async Task<TokenGrainDto> GetTokenAsync(string chainId, string symbol)
    {
        try
        {
            var grainId = GuidHelper.GenerateGrainId(chainId, symbol);
            var tokenGrain = _clusterClient.GetGrain<ITokenGrain>(grainId);
            var grainResultDto = await tokenGrain.GetTokenAsync(new TokenGrainDto
            {
                ChainId = chainId,
                Symbol = symbol
            });
            AssertHelper.IsTrue(grainResultDto.Success, "GetTokenAsync  fail, chainId  {chainId} symbol {symbol}", chainId,
                symbol);
            return grainResultDto.Data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTokenAsyncException chainId{chainId}, symbol{symbol}", chainId, symbol);
        }

        return new TokenGrainDto();
    }

    public async Task<TokenDto> GetTokenByExplorerAsync(string chainId, string symbol)
    {
        var tokenGrain = await GetTokenAsync(chainId, symbol);
        var tokenInfo = await _explorerProvider.GetTokenInfoAsync(chainId, new ExplorerTokenInfoRequest()
        {
            Symbol = symbol
        });
        var tokenResult = _objectMapper.Map<ExplorerTokenInfoResponse, TokenDto>(tokenInfo);
        tokenResult.ImageUrl = tokenGrain.ImageUrl;
        return tokenResult;
    }

    public async Task<double> GetTvlAsync(string chainId)
    {
        var list = await _graphQlProvider.GetDAOAmountAsync(chainId);
        var tokens = list.Where(x => x.Amount > 0).Where(x => !string.IsNullOrEmpty(x.GovernanceToken))
            .Select(x => x.GovernanceToken).Distinct().ToList();
        var tokenInfoTasks = tokens.Select(x => _explorerProvider.GetTokenInfoAsync(chainId, x)).ToList();
        var priceTasks = tokens.Select(x => GetTokenPriceAsync(x, CommonConstant.USD)).ToList();
        var tokenInfoResults = (await Task.WhenAll(tokenInfoTasks)).ToDictionary(x => x.Symbol, x => x); 
        var priceResults = (await Task.WhenAll(priceTasks)).ToDictionary(x => x.BaseCoin, x => x);
        var sum = list.Where(x => x.Amount > 0).Sum(x => 
            x.Amount / Math.Pow(10, Convert.ToDouble(tokenInfoResults.GetValueOrDefault(x.GovernanceToken)?.Decimals ?? "0")) 
            * (double)(priceResults.GetValueOrDefault(x.GovernanceToken)?.Price ?? 0));
        return sum;
    }

    public async Task<TokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin)
    {
        if (baseCoin.IsNullOrEmpty() || quoteCoin.IsNullOrEmpty())
        {
            _logger.LogError("GetTokenPriceAsync Get token price fail, baseCoin or quoteCoin is empty.");
            return new TokenPriceDto { BaseCoin = baseCoin, QuoteCoin = quoteCoin, Price = 0 };
        }
        var exchange = await GetExchangePriceAsync(baseCoin, quoteCoin);
        if (exchange.IsNullOrEmpty())
        {
            _logger.LogError("GetTokenPriceAsync Exchange data not found baseCoin {baseCoin} quoteCoin {quoteCoin}", baseCoin, quoteCoin);
            return new TokenPriceDto { BaseCoin = baseCoin, QuoteCoin = quoteCoin, Price = 0 };
        }
        var avgExchange = exchange.Values
            .Where(ex => ex.Exchange > 0)
            .Average(ex => ex.Exchange);
        return new TokenPriceDto { BaseCoin = baseCoin, QuoteCoin = quoteCoin, Price = avgExchange };
    }

    public async Task<Dictionary<string, TokenExchangeDto>> GetExchangePriceAsync(string baseCoin, string quoteCoin)
    {
        var pair = string.Join(CommonConstant.Underline, baseCoin, quoteCoin);
        var exchangeGrain = _clusterClient.GetGrain<ITokenExchangeGrain>(pair);
        var now = DateTime.UtcNow.ToUtcMilliSeconds();
        var exchange = await exchangeGrain.GetAsync();
        if (exchange.LastModifyTime > 0 && exchange.ExpireTime > now)
        {
            return exchange.ExchangeInfos;
        }

        var asyncTasks = new Dictionary<string, Task<TokenExchangeDto>>();
        foreach (var provider in _exchangeProviders.Values)
        {
            asyncTasks[provider.Name().ToString()] =
                provider.LatestAsync(MappingSymbol(baseCoin.ToUpper()), MappingSymbol(quoteCoin.ToUpper()));
        }
        
        exchange.LastModifyTime = now;
        exchange.ExpireTime = now + _exchangeOptions.CurrentValue.DataExpireSeconds * 1000;
        exchange.ExchangeInfos = new Dictionary<string, TokenExchangeDto>();
        foreach (var (providerName, exchangeTask) in asyncTasks)
        {
            try
            {
                exchange.ExchangeInfos.Add(providerName, await exchangeTask);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Query exchange failed, providerName={ProviderName}", providerName);
            }
        }
        await exchangeGrain.SetAsync(exchange);

        return exchange.ExchangeInfos;
    }
    
    private string MappingSymbol(string sourceSymbol)
    {
        return _netWorkReflectionOption.CurrentValue.SymbolItems.TryGetValue(sourceSymbol, out var targetSymbol)
            ? targetSymbol
            : sourceSymbol;
    }
}