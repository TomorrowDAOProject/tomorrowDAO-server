using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Dto;
using TomorrowDAOServer.Treasury.Dto;
using TomorrowDAOServer.Treasury.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Treasury;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TreasuryAssetsService : TomorrowDAOServerAppService, ITreasuryAssetsService
{
    private readonly ILogger<TreasuryAssetsService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly ITreasuryAssetsProvider _treasuryAssetsProvider;
    private readonly ITokenService _tokenService;

    public TreasuryAssetsService(ILogger<TreasuryAssetsService> logger, ITreasuryAssetsProvider treasuryAssetsProvider,
        IObjectMapper objectMapper, ITokenService tokenService)
    {
        _logger = logger;
        _treasuryAssetsProvider = treasuryAssetsProvider;
        _objectMapper = objectMapper;
        _tokenService = tokenService;
    }

    public async Task<TreasuryAssetsPagedResultDto> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input)
    {
        try
        {
            if (input == null || input.DaoId.IsNullOrWhiteSpace() || input.ChainId.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("request parameters daoId or chainId cannot be empty.");
            }

            var resultDto = new TreasuryAssetsPagedResultDto();
            var treasuryAssetsResult = await _treasuryAssetsProvider.GetAllTreasuryAssetsAsync(
                new GetAllTreasuryAssetsInput
                {
                    ChainId = input.ChainId, DaoId = input.DaoId
                });
            if (treasuryAssetsResult.Item1 <= 0)
            {
                return resultDto;
            }

            var allTreasuryFunds = treasuryAssetsResult.Item2.OrderBy(x => x.AvailableFunds).ToList();
            var treasuryFund = allTreasuryFunds.FirstOrDefault();
            var rangeTreasuryFunds = GetRangeTreasuryFunds(input.SkipCount, input.MaxResultCount, allTreasuryFunds);

            resultDto.TreasuryAddress = treasuryFund?.TreasuryAddress;
            resultDto.DaoId = treasuryFund?.DaoId;
            resultDto.Data = _objectMapper.Map<List<TreasuryFundDto>, List<TreasuryAssetsDto>>(rangeTreasuryFunds);
            resultDto.TotalCount = treasuryAssetsResult.Item1;

            var symbols = allTreasuryFunds.Where(x => !string.IsNullOrEmpty(x.Symbol)).Select(x => x.Symbol)
                .ToHashSet();
            var (tokenInfoDictionary, tokenPriceDictionary) = await GetTokenInfoAsync(input.ChainId, symbols);

            foreach (var dto in resultDto.Data.Where(dto => tokenInfoDictionary.ContainsKey(dto.Symbol)))
            {
                dto.Decimal = tokenInfoDictionary[dto.Symbol].Decimals;
                dto.UsdValue = dto.Amount / Math.Pow(10, dto.Decimal) *
                               (double)(tokenPriceDictionary.GetValueOrDefault(dto.Symbol)?.Price ?? 0);
            }

            resultDto.TotalUsdValue =
                (from dto in allTreasuryFunds.Where(x => tokenInfoDictionary.ContainsKey(x.Symbol))
                    let symbolDecimal = tokenInfoDictionary[dto.Symbol].Decimals
                    select dto.AvailableFunds / Math.Pow(10, symbolDecimal) *
                           (double)(tokenPriceDictionary.GetValueOrDefault(dto.Symbol)?.Price ?? 0))
                .Sum();

            return resultDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get treasury assets error. daoId={0}, chainId={1}", input?.DaoId, input?.ChainId);
            throw;
        }
    }

    public async Task<double> GetTreasuryAssetsAmountAsync(GetTreasuryAssetsInput input,
        Tuple<Dictionary<string, TokenGrainDto>, Dictionary<string, TokenPriceDto>> tokenInfo)
    {
        try
        {
            if (input == null || input.DaoId.IsNullOrWhiteSpace() || input.ChainId.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("request parameters daoId or chainId cannot be empty.");
            }

            var resultDto = new TreasuryAssetsPagedResultDto();
            var treasuryAssetsResult = await _treasuryAssetsProvider.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput
            {
                MaxResultCount = input.MaxResultCount,
                SkipCount = input.SkipCount,
                ChainId = input.ChainId,
                Symbols = input.Symbols,
                DaoId = input.DaoId
            });
            if (treasuryAssetsResult.Item2.IsNullOrEmpty())
            {
                return 0;
            }

            var treasuryFunds = treasuryAssetsResult.Item2;

            var tokenInfoDictionary = tokenInfo.Item1;
            var tokenPriceDictionary = tokenInfo.Item2;

            var totalUsdValue =
                (from dto in treasuryFunds.Where(x => tokenInfoDictionary.ContainsKey(x.Symbol))
                    let symbolDecimal = tokenInfoDictionary[dto.Symbol].Decimals
                    select dto.AvailableFunds / Math.Pow(10, symbolDecimal) *
                           (double)(tokenPriceDictionary.GetValueOrDefault(dto.Symbol)?.Price ?? 0))
                .Sum();

            return totalUsdValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get treasury assets error. daoId={0}, chainId={1}", input?.DaoId, input?.ChainId);
            throw;
        }
    }

    private static List<TreasuryFundDto> GetRangeTreasuryFunds(int skipCount, int maxResult,
        List<TreasuryFundDto> allFunds)
    {
        return skipCount >= allFunds.Count
            ? new List<TreasuryFundDto>()
            : allFunds.GetRange(skipCount, Math.Min(maxResult, allFunds.Count - skipCount));
    }

    public async Task<Tuple<Dictionary<string, TokenGrainDto>, Dictionary<string, TokenPriceDto>>> GetTokenInfoAsync(
        string chainId, ISet<string> symbols)
    {
        var tokenInfoTasks = symbols.Select(symbol => _tokenService.GetTokenAsync(chainId, symbol)).ToList();
        var tokenPriceTasks = symbols.Select(symbol => _tokenService.GetTokenPriceAsync(symbol, CommonConstant.USD))
            .ToList();
        var tokenInfoResults = (await Task.WhenAll(tokenInfoTasks)).Where(x => x != null)
            .ToDictionary(x => x.Symbol, x => x);
        var priceResults = (await Task.WhenAll(tokenPriceTasks)).Where(x => x != null)
            .ToDictionary(x => x.BaseCoin, x => x);
        return new Tuple<Dictionary<string, TokenGrainDto>, Dictionary<string, TokenPriceDto>>(tokenInfoResults,
            priceResults);
    }
}