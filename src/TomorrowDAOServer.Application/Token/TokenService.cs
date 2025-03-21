using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ExceptionHandler;
using Aetherlink.PriceServer.Common;
using JetBrains.Annotations;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Token.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Aws;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Dtos.AelfScan;
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
    private readonly IContractProvider _contractProvider;
    private readonly IAwsS3Client _awsS3Client;
    
    public TokenService(IClusterClient clusterClient, ILogger<TokenService> logger, IExplorerProvider explorerProvider, 
        IObjectMapper objectMapper, IGraphQLProvider graphQlProvider, IEnumerable<IExchangeProvider> exchangeProviders,
        IOptionsMonitor<NetWorkReflectionOptions> netWorkReflectionOption, IOptionsMonitor<ExchangeOptions> exchangeOptions, 
        IContractProvider contractProvider, IAwsS3Client awsS3Client)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _explorerProvider = explorerProvider;
        _objectMapper = objectMapper;
        _graphQlProvider = graphQlProvider;
        _exchangeProviders = exchangeProviders.ToDictionary(p => p.Name().ToString());
        _netWorkReflectionOption = netWorkReflectionOption;
        _exchangeOptions = exchangeOptions;
        _contractProvider = contractProvider;
        _awsS3Client = awsS3Client;
    }

    public async Task<TokenInfoDto> GetTokenInfoAsync(string chainId, string symbol)
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        if (symbol.IsNullOrEmpty())
        {
            return new TokenInfoDto();
        }

        var tokenInfo = await _graphQlProvider.GetTokenInfoAsync(chainId, symbol.ToUpper());
        if (DateTime.UtcNow.ToUtcMilliSeconds() - tokenInfo.LastUpdateTime <= CommonConstant.OneDay)
        {
            sw.Stop();
            Log.Information("ProposalListDuration: GetTokenInfoAsync {0}", sw.ElapsedMilliseconds);
            
            return tokenInfo;
        }

        var tokenResponse = await _explorerProvider.GetTokenInfoAsync(new GetTokenInfoFromAelfScanRequest
        {
            ChainId = chainId,
            Symbol = symbol.ToUpper()
        });
        if (tokenResponse == null || tokenResponse.Symbol.IsNullOrWhiteSpace())
        {
            sw.Stop();
            Log.Information("ProposalListDuration: ExplorerGetTokenInfoAsync {0}", sw.ElapsedMilliseconds);
            return tokenInfo;
        }

        tokenResponse.ChainId = chainId;
        tokenResponse.Id = IdGeneratorHelper.GenerateId(chainId, symbol);
        tokenInfo = _objectMapper.Map<GetTokenInfoFromAelfScanResponse, TokenInfoDto>(tokenResponse);
        tokenInfo.LastUpdateTime = DateTime.UtcNow.ToUtcMilliSeconds();
        await _graphQlProvider.SetTokenInfoAsync(tokenInfo);
        
        return tokenInfo;
    }

    public async Task<TokenInfoDto> GetTokenInfoWithoutUpdateAsync(string chainId, string symbol)
    {
        if (symbol.IsNullOrEmpty())
        {
            return new TokenInfoDto();
        }

        return await _graphQlProvider.GetTokenInfoAsync(chainId, symbol.ToUpper());
    }

    public async Task<TvlDetail> GetTvlAsync(string chainId)
    {
        var list = await _graphQlProvider.GetDAOAmountAsync(chainId);
        var tokens = list.Where(x => x.Amount > 0).Where(x => !string.IsNullOrEmpty(x.GovernanceToken))
            .Select(x => x.GovernanceToken).Distinct().ToList();
        var tokenInfoTasks = tokens.Select(x => GetTokenInfoAsync(chainId, x)).ToList();
        var priceTasks = tokens.Select(x => GetTokenPriceAsync(x, CommonConstant.USD)).ToList();
        var tokenInfoResults = (await Task.WhenAll(tokenInfoTasks)).ToDictionary(x => x.Symbol, x => x); 
        var priceResults = (await Task.WhenAll(priceTasks)).ToDictionary(x => x.BaseCoin, x => x);
        var detail = list.Where(x => x.Amount > 0).Select(x => new TokenTvl
        {
            Symbol = x.GovernanceToken,
            Tvl = x.Amount / Math.Pow(10, Convert.ToDouble(tokenInfoResults.GetValueOrDefault(x.GovernanceToken)?.Decimals ?? "0"))
                  * (double)(priceResults.GetValueOrDefault(x.GovernanceToken)?.Price ?? 0)
        }).Where(x => x.Tvl > 0).ToList();
        var sum = detail.Sum(x => x.Tvl);
        return new TvlDetail
        {
            Tvl = sum,
            Detail = detail
        };
    }

    public async Task<TokenPriceDto> GetTokenPriceAsync(string baseCoin, string quoteCoin)
    {
        if (baseCoin.IsNullOrEmpty() || quoteCoin.IsNullOrEmpty())
        {
            return new TokenPriceDto { BaseCoin = baseCoin, QuoteCoin = quoteCoin, Price = 0 };
        }
        var exchangeGrain = _clusterClient.GetGrain<ITokenExchangeGrain>(string.Join(CommonConstant.Underline, baseCoin, quoteCoin));
        var exchange = await exchangeGrain.GetAsync();
        return new TokenPriceDto { BaseCoin = baseCoin, QuoteCoin = quoteCoin, Price = await AvgPrice(exchange) };
    }

    public async Task UpdateExchangePriceAsync(string baseCoin, string quoteCoin,  List<ExchangeProviderName> providerNames)
    {
        var pair = string.Join(CommonConstant.Underline, baseCoin, quoteCoin);
        var exchangeGrain = _clusterClient.GetGrain<ITokenExchangeGrain>(pair);
        var now = DateTime.UtcNow.ToUtcMilliSeconds();
        var exchange = await exchangeGrain.GetAsync();
        var asyncTasks = new Dictionary<string, Task<TokenExchangeDto>>();
        IEnumerable<IExchangeProvider> filteredProviders = _exchangeProviders.Values;
        if (!providerNames.IsNullOrEmpty())
        {
            filteredProviders = filteredProviders.Where(x => providerNames.Contains(x.Name())).ToList();
        }
        
        foreach (var provider in filteredProviders)
        {
            asyncTasks[provider.Name().ToString()] =
                provider.LatestAsync(MappingSymbol(baseCoin.ToUpper()), MappingSymbol(quoteCoin.ToUpper()));
        }
        exchange.LastModifyTime = now;
        exchange.ExpireTime = now + _exchangeOptions.CurrentValue.DataExpireSeconds * 1000;
        exchange.ExchangeInfos = new Dictionary<string, TokenExchangeDto>();
        foreach (var (providerName, exchangeTask) in asyncTasks)
        {
            //await UpdateExchangeInfosAsync(exchange, providerName, exchangeTask);
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
        Log.Debug("UpdateExchangePriceAsync pair {pair}, exchange {exchange}", 
            pair, JsonConvert.SerializeObject(exchange));
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler), 
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), 
        Message = "Query exchange failed", LogTargets = new []{"providerName"})]
    public virtual async Task UpdateExchangeInfosAsync(TokenExchangeGrainDto exchange, string providerName,
        Task<TokenExchangeDto> exchangeTask)
    {
        exchange.ExchangeInfos.Add(providerName, await exchangeTask);
    }


    private string MappingSymbol(string sourceSymbol)
    {
        return _netWorkReflectionOption.CurrentValue.SymbolItems.TryGetValue(sourceSymbol, out var targetSymbol)
            ? targetSymbol : sourceSymbol;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler), 
        MethodName = nameof(TmrwDaoExceptionHandler.HandleAvgPrice))]
    public virtual async Task<decimal> AvgPrice([CanBeNull] TokenExchangeGrainDto exchange)
    {
        if (exchange == null || exchange.ExchangeInfos == null || !exchange.ExchangeInfos.Any())
        {
            return 0;
        }
        var validExchanges = exchange.ExchangeInfos.Values
            .Where(ex => ex.Exchange > 0)
            .ToList();

        return validExchanges.Any() ? validExchanges.Average(ex => ex.Exchange) : 0;
    }
}