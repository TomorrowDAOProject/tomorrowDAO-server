using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.NetworkDao;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Dto;
using TomorrowDAOServer.Treasury.Dto;
using TomorrowDAOServer.Treasury.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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
    private readonly IDAOProvider _daoProvider;
    private readonly INetworkDaoTreasuryService _networkDaoTreasuryService;
    private readonly IContractProvider _contractProvider;

    public TreasuryAssetsService(ILogger<TreasuryAssetsService> logger, ITreasuryAssetsProvider treasuryAssetsProvider,
        IObjectMapper objectMapper, ITokenService tokenService, IDAOProvider daoProvider,
        INetworkDaoTreasuryService networkDaoTreasuryService, IContractProvider contractProvider)
    {
        _logger = logger;
        _treasuryAssetsProvider = treasuryAssetsProvider;
        _objectMapper = objectMapper;
        _tokenService = tokenService;
        _daoProvider = daoProvider;
        _networkDaoTreasuryService = networkDaoTreasuryService;
        _contractProvider = contractProvider;
    }

    public async Task<TreasuryAssetsPagedResultDto> GetTreasuryAssetsAsync(GetTreasuryAssetsInput input)
    {
        if (input == null || (input.DaoId.IsNullOrWhiteSpace() && input.Alias.IsNullOrWhiteSpace()) ||
            input.ChainId.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }
        
        try
        {
            var resultDto = new TreasuryAssetsPagedResultDto();
            var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
            {
                ChainId = input.ChainId,
                DAOId = input.DaoId,
                Alias = input.Alias
            });
            if (daoIndex == null || daoIndex.Id.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("No DAO information found.");
            }

            input.DaoId = daoIndex.Id;

            if (daoIndex.IsNetworkDAO)
            {
                var response = await _networkDaoTreasuryService.GetBalanceAsync(new TreasuryBalanceRequest
                    { ChainId = CommonConstant.MainChainId });
                resultDto.TotalUsdValue = response.Items.Sum(x => Convert.ToDouble(x.DollarValue));
                resultDto.TotalCount = response.Items.Count;
                resultDto.Data = _objectMapper.Map<List<TreasuryBalanceResponse.BalanceItem>, List<TreasuryAssetsDto>>(
                    response.Items.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
                return resultDto;
            }

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
                dto.Decimal = tokenInfoDictionary[dto.Symbol].Decimals.SafeToInt();
                dto.UsdValue = dto.Amount / Math.Pow(10, dto.Decimal) *
                               (double)(tokenPriceDictionary.GetValueOrDefault(dto.Symbol)?.Price ?? 0);
            }

            resultDto.TotalUsdValue =
                (from dto in allTreasuryFunds.Where(x => tokenInfoDictionary.ContainsKey(x.Symbol))
                    let symbolDecimal = tokenInfoDictionary[dto.Symbol].Decimals
                    select dto.AvailableFunds / Math.Pow(10, symbolDecimal.SafeToDouble()) *
                           (double)(tokenPriceDictionary.GetValueOrDefault(dto.Symbol)?.Price ?? 0))
                .Sum();

            return resultDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get treasury assets error. daoId={0}, chainId={1}", input?.DaoId, input?.ChainId);
            throw new UserFriendlyException($"System exception occurred during querying treasury assets. {e.Message}");
        }
    }

    public async Task<bool> IsTreasuryDepositorAsync(IsTreasuryDepositorInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.TreasuryAddress.IsNullOrWhiteSpace() ||
            input.Address.IsNullOrWhiteSpace() || input.GovernanceToken.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        try
        {
            var result = await _treasuryAssetsProvider.GetTreasuryRecordListAsync(
                new GetTreasuryRecordListInput
                {
                    MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                    SkipCount = 0,
                    ChainId = input.ChainId,
                    TreasuryAddress = input.TreasuryAddress,
                    Address = input.Address,
                    Symbols = new List<string>() { input.GovernanceToken }
                });
            return result != null && !result.Item2.IsNullOrEmpty();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "exec IsTreasuryDepositorAsync error. {0}", JsonConvert.SerializeObject(input));
            throw new UserFriendlyException(
                $"An exception occurred when running the IsTreasuryDepositor method, {e.Message}");
        }
    }

    public async Task<string> GetTreasuryAddressAsync(GetTreasuryAddressInput input)
    {
        if (input == null || (input.DaoId.IsNullOrWhiteSpace() && input.Alias.IsNullOrWhiteSpace()))
        {
            ExceptionHelper.ThrowArgumentException();
        }

        try
        {
            if (input.DaoId.IsNullOrWhiteSpace())
            {
                var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
                {
                    ChainId = input.ChainId,
                    Alias = input.Alias
                });
                if (daoIndex == null || daoIndex.Id.IsNullOrWhiteSpace())
                {
                    throw new UserFriendlyException("No DAO information found.");
                }

                input.DaoId = daoIndex.Id;
            }

            return await _contractProvider.GetTreasuryAddressAsync(input.ChainId, input.DaoId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTreasuryAddressAsync error, {0}", JsonConvert.SerializeObject(input));
            throw new UserFriendlyException($"System exception occurred during querying treasury address. {e.Message}");
        }
    }

    public async Task<PageResultDto<TreasuryRecordDto>> GetTreasuryRecordsAsync(GetTreasuryRecordsInput input)
    {
        var result = await _treasuryAssetsProvider.GetTreasuryRecordListAsync(new GetTreasuryRecordListInput
        {
            MaxResultCount = input.MaxResultCount, SkipCount = input.SkipCount,
            ChainId = input.ChainId, TreasuryAddress = input.TreasuryAddress,
        });
        var records = result.Item2;
        var symbols = records.Select(x => x.Symbol).Distinct().ToList();
        var tasks = symbols.Select(symbol => _tokenService.GetTokenInfoAsync(input.ChainId, symbol)).ToList();
        await Task.WhenAll(tasks);
        var tokenInfoDic = tasks.Select(x => x.Result)
            .Where(x => x != null && !string.IsNullOrEmpty(x.Symbol))
            .ToDictionary(x => x.Symbol, x => x);
        foreach (var record in records)
        {
            record.TransactionId = record.OfTransactionId(record.Id);
            if (tokenInfoDic.TryGetValue(record.Symbol, out var tokenInfo))
            {
                record.Decimals = tokenInfo.Decimals;
                record.AmountAfterDecimals = record.Amount / Math.Pow(10, record.Decimals.SafeToInt());
            }
        }
        return new PageResultDto<TreasuryRecordDto>
        {
            TotalCount = result.Item1, Data = records
        };
    }

    private static List<TreasuryFundDto> GetRangeTreasuryFunds(int skipCount, int maxResult,
        List<TreasuryFundDto> allFunds)
    {
        return skipCount >= allFunds.Count
            ? new List<TreasuryFundDto>()
            : allFunds.GetRange(skipCount, Math.Min(maxResult, allFunds.Count - skipCount));
    }

    private async Task<Tuple<Dictionary<string, TokenInfoDto>, Dictionary<string, TokenPriceDto>>> GetTokenInfoAsync(
        string chainId, IReadOnlyCollection<string> symbols)
    {
        var tokenInfoTasks = symbols.Select(symbol => _tokenService.GetTokenInfoAsync(chainId, symbol)).ToList();
        var tokenPriceTasks = symbols.Select(symbol => _tokenService.GetTokenPriceAsync(symbol, CommonConstant.USD))
            .ToList();
        var tokenInfoResults = (await Task.WhenAll(tokenInfoTasks)).Where(x => x != null && !string.IsNullOrEmpty(x.Symbol))
            .ToDictionary(x => x.Symbol, x => x);
        var priceResults = (await Task.WhenAll(tokenPriceTasks)).Where(x => x != null && !string.IsNullOrEmpty(x.BaseCoin))
            .ToDictionary(x => x.BaseCoin, x => x);
        return new Tuple<Dictionary<string, TokenInfoDto>, Dictionary<string, TokenPriceDto>>(tokenInfoResults,
            priceResults);
    }
}