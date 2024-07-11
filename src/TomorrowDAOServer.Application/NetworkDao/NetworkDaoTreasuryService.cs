using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Token;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoTreasuryService : INetworkDaoTreasuryService, ITransientDependency
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
        var balance = await _explorerProvider.GetBalancesAsync(request.ChainId, new ExplorerBalanceRequest
        {
            Address = treasuryContractAddress
        });

        var balanceItems = balance.Select(b =>
        {
            var token = AsyncHelper.RunSync(() => _tokenService.GetTokenAsync(request.ChainId, b.Symbol));
            var exchange = _networkDaoOptions.CurrentValue.PopularSymbols.Contains(b.Symbol)
                ? AsyncHelper.RunSync(() => _tokenService.GetTokenPriceAsync(b.Symbol, CommonConstant.USDT))
                : null;
            return new TreasuryBalanceResponse.BalanceItem
            {
                TotalCount = b.Balance,
                DollarValue = exchange == null ? null : (b.Balance.SafeToDecimal() * exchange.Price).ToString(2),
                Token = new TokenDto
                {
                    Symbol = b.Symbol,
                    Name = token.TokenName,
                    Decimals = token.Decimals,
                    ImageUrl = _tokenOptions.CurrentValue.TokenInfos.TryGetValue(b.Symbol, out var img)
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


    public async Task<PagedResultDto<TreasuryTransactionDto>> GetTreasuryTransactionAsync(
        TreasuryTransactionRequest request)
    {
        var treasuryContractAddress =
            _contractProvider.ContractAddress(request.ChainId, SystemContractName.TreasuryContract);
        AssertHelper.NotEmpty(treasuryContractAddress, "Treasury contract address empty");
        
        var explorerResult = await _explorerProvider.GetTransferListAsync(request.ChainId,
            new ExplorerTransferRequest(request)
            {
                Address = treasuryContractAddress
            });
        var items =
            _objectMapper.Map<List<ExplorerTransferResult>, List<TreasuryTransactionDto>>(explorerResult.List);
        
        return new PagedResultDto<TreasuryTransactionDto>
        {
            TotalCount = explorerResult.Total,
            Items = items
        };
    }
    
}