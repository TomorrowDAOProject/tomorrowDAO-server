using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Dtos.AelfScan;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Token;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoTreasuryService : TomorrowDAOServerAppService, INetworkDaoTreasuryService
{
    private readonly ILogger<NetworkDaoTreasuryService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IOptionsMonitor<NetworkDaoOptions> _networkDaoOptions;
    private readonly IOptionsMonitor<TokenInfoOptions> _tokenOptions;
    private readonly ITokenService _tokenService;
    private readonly IContractProvider _contractProvider;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IObjectMapper _objectMapper;

    public NetworkDaoTreasuryService(ILogger<NetworkDaoTreasuryService> logger, IContractProvider contractProvider,
        IExplorerProvider explorerProvider, ITokenService tokenService, IClusterClient clusterClient,
        IOptionsMonitor<NetworkDaoOptions> networkDaoOptions, IOptionsMonitor<TokenInfoOptions> tokenOptions,
        IObjectMapper objectMapper)
    {
        _logger = logger;
        _contractProvider = contractProvider;
        _explorerProvider = explorerProvider;
        _tokenService = tokenService;
        _clusterClient = clusterClient;
        _networkDaoOptions = networkDaoOptions;
        _tokenOptions = tokenOptions;
        _objectMapper = objectMapper;
    }


    public async Task<TreasuryBalanceResponse> GetBalanceAsync(TreasuryBalanceRequest request)
    {
        var treasuryContractAddress =
            _contractProvider.ContractAddress(request.ChainId, SystemContractName.TreasuryContract);
        var balance = await _explorerProvider.GetBalancesAsync(new GetBalanceFromAelfScanRequest()
        {
            ChainId = request.ChainId,
            Address = treasuryContractAddress
        });

        var balanceItems = balance.Select(b =>
        {
            var symbol = b?.Token?.Symbol ?? string.Empty;
            var balance = b?.Quantity ?? decimal.Zero;
            var token = AsyncHelper.RunSync(() => _tokenService.GetTokenInfoAsync(request.ChainId, symbol));
            var exchange = _networkDaoOptions.CurrentValue.PopularSymbols.Contains(symbol)
                ? AsyncHelper.RunSync(() => _tokenService.GetTokenPriceAsync(symbol, CommonConstant.USDT))
                : null;
            return new TreasuryBalanceResponse.BalanceItem
            {
                TotalCount = balance.ToString(),
                DollarValue = exchange == null ? null : (balance * exchange.Price).ToString(2),
                Token = new TokenDto
                {
                    Symbol = symbol,
                    Name = token.Name,
                    Decimals = Convert.ToInt32(token.Decimals),
                    ImageUrl = _tokenOptions.CurrentValue.TokenInfos.TryGetValue(symbol, out var img)
                        ? img.ImageUrl
                        : null
                }
            };
        }).ToList();

        return new TreasuryBalanceResponse
        {
            ContractAddress = _contractProvider.ContractAddress(request.ChainId, SystemContractName.TreasuryContract),
            Items = balanceItems
        };
    }
}