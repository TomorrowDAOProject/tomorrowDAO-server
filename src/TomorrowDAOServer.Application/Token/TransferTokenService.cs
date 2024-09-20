using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Token.Dto;
using TomorrowDAOServer.Token.Provider;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Token;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TransferTokenService : TomorrowDAOServerAppService, ITransferTokenService
{
    private readonly ILogger<TransferTokenService> _logger;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUserProvider _userProvider;
    private readonly IOptionsMonitor<TransferTokenOption> _transferTokenOptions;
    private readonly ITransferTokenProvider _transferTokenProvider;

    private readonly SenderAccount _senderAccount;


    private const string DistributedLockPrefix = "TokenClaimLock";
    private const string DistributedCachePrefix = "TokenClaimRecord";

    public TransferTokenService(ILogger<TransferTokenService> logger, IDistributedCache<string> distributedCache,
        IAbpDistributedLock distributedLock, IUserProvider userProvider,
        IOptionsMonitor<TransferTokenOption> transferTokenOptions, ITransferTokenProvider transferTokenProvider)
    {
        _logger = logger;
        _distributedCache = distributedCache;
        _distributedLock = distributedLock;
        _userProvider = userProvider;
        _transferTokenOptions = transferTokenOptions;
        _transferTokenProvider = transferTokenProvider;

        _senderAccount = new SenderAccount(transferTokenOptions.CurrentValue.SenderAccount);
    }

    public async Task<TransferTokenResponse> TransferTokenAsync(TransferTokenInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.Symbol.IsNullOrWhiteSpace() ||
            !_transferTokenOptions.CurrentValue.SupportedSymbol.Contains(input.Symbol))
        {
            ExceptionHelper.ThrowArgumentException();
        }

        try
        {
            _logger.LogInformation("Transfer token, start...");

            var userId = CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty;
            _logger.LogInformation("TransferTokenUserId {0}", userId);
            var address = await _userProvider.GetAndValidateUserAddressAsync(userId, input.ChainId);
            if (address.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("User Address Not Found.");
            }

            _logger.LogInformation("Transfer token, query transfer record. {0}", address);
            var claimRecord = await GetTokenClaimRecordAsync(input.ChainId, address, input.Symbol);
            if (claimRecord != null)
            {
                _logger.LogInformation("Transfer token, has Transferred. {0}", address);
                return BuildTransferTokenResponse(TransferTokenStatus.AlreadyClaimed);
            }

            _logger.LogInformation("Transfer token, Lock user. {0}", address);
            var distributedLockKey = GenerateDistributedLockKey(input.ChainId, address, input.Symbol);
            using (var lockHandle = _distributedLock.TryAcquireAsync(distributedLockKey,
                       _transferTokenOptions.CurrentValue.GetLockUserTimeoutTimeSpan()))
            {
                if (lockHandle == null)
                {
                    _logger.LogInformation("Transfer token, lock failed. {0}", address);
                    return BuildTransferTokenResponse(TransferTokenStatus.AlreadyClaimed);
                }

                _logger.LogInformation("Transfer token, query transfer record again. {0}", address);
                claimRecord = await GetTokenClaimRecordAsync(input.ChainId, address, input.Symbol);
                if (claimRecord != null)
                {
                    _logger.LogInformation("Transfer token, has Transferred. {0}", address);
                    return BuildTransferTokenResponse(TransferTokenStatus.AlreadyClaimed);
                }

                _logger.LogInformation("Transfer token, query transfer record from chain. {0}", address);
                var balance = await _transferTokenProvider.GetBalanceAsync(input.ChainId, input.Symbol, address);
                if (balance.Balance > GetOneTokenAmount(input.Symbol))
                {
                    _logger.LogInformation("Transfer token, already holding tokens. {0}", address);
                    await SaveTokenClaimRecordAsync(null, TransferTokenStatus.AlreadyClaimed,
                        input.ChainId, address, input.Symbol);
                    return BuildTransferTokenResponse(TransferTokenStatus.AlreadyClaimed);
                }

                _logger.LogInformation("Transfer token, send transfer transaction. {0}", address);
                var result = await _transferTokenProvider.TransferTokenAsync(_senderAccount, input.ChainId,
                    input.Symbol,
                    address, GetOneTokenAmount(input.Symbol));

                if (result.TransactionId.IsNullOrWhiteSpace())
                {
                    _logger.LogError("Transfer token, send transaction error, {0}",
                        JsonConvert.SerializeObject(result));
                    return BuildTransferTokenResponse(TransferTokenStatus.TransferFailed);
                }

                _logger.LogInformation("Transfer token, send transfer transaction success. {0}", address);
                await SaveTokenClaimRecordAsync(result.TransactionId, TransferTokenStatus.TransferInProgress,
                    input.ChainId, address, input.Symbol,
                    _transferTokenOptions.CurrentValue.GetTransferTimeoutTimeSpan());

                var _ = UpdateTransactionStatusAsync(input.ChainId, address, input.Symbol, result.TransactionId);

                return BuildTransferTokenResponse(TransferTokenStatus.TransferInProgress);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TransferTokenAsync error, {0}", JsonConvert.SerializeObject(input));
            ExceptionHelper.ThrowSystemException("transferring token", e);
            return BuildTransferTokenResponse(TransferTokenStatus.TransferFailed);
        }
    }

    public async Task<TokenClaimRecord> GetTransferTokenStatusAsync(TransferTokenStatusInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.Address.IsNullOrWhiteSpace() ||
            input.Symbol.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        return await GetTokenClaimRecordAsync(input!.ChainId, input.Address, input.Symbol);
    }

    private async Task<TokenClaimRecord> GetTokenClaimRecordAsync(string chainId, string address, string symbol)
    {
        var distributeCacheKey = GenerateDistributeCacheKey(chainId, address, symbol);
        var cache = await _distributedCache.GetAsync(distributeCacheKey);
        return cache.IsNullOrWhiteSpace() ? null : JsonConvert.DeserializeObject<TokenClaimRecord>(cache);
    }

    private async Task SaveTokenClaimRecordAsync(
        string transactionId,
        TransferTokenStatus status, string chainId,
        string address, string symbol, TimeSpan? expire = null)
    {
        await SaveTokenClaimRecordAsync(new TokenClaimRecord
        {
            TransactionId = transactionId,
            ClaimTime = DateTime.Now.ToUtcString(TimeHelper.DefaultPattern),
            Status = status,
            IsClaimedInSystem = !transactionId.IsNullOrWhiteSpace()
        }, chainId, address, symbol, expire);
    }

    private async Task SaveTokenClaimRecordAsync(TokenClaimRecord record, string chainId, string address,
        string symbol, TimeSpan? expire = null)
    {
        var distributeCacheKey = GenerateDistributeCacheKey(chainId, address, symbol);
        await _distributedCache.SetAsync(distributeCacheKey, JsonConvert.SerializeObject(record),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expire ?? TimeSpan.FromHours(87600),
            });
    }

    private TransferTokenResponse BuildTransferTokenResponse(TransferTokenStatus status)
    {
        return new TransferTokenResponse
        {
            Status = status
        };
    }

    private string GenerateDistributedLockKey(string chainId, string address, string symbol)
    {
        return $"{DistributedLockPrefix}:{chainId}:{address}:{symbol}";
    }

    private string GenerateDistributeCacheKey(string chainId, string address, string symbol)
    {
        return $"{DistributedCachePrefix}:{chainId}:{address}:{symbol}";
    }

    private long GetOneTokenAmount(string symbol)
    {
        int symbolDecimal = 0;
        if (_transferTokenOptions.CurrentValue.SymbolDecimal.ContainsKey(symbol))
        {
            symbolDecimal = _transferTokenOptions.CurrentValue.SymbolDecimal[symbol];
        }

        return (long)Math.Pow(10, symbolDecimal);
    }

    private async Task UpdateTransactionStatusAsync(string chainId, string address,
        string symbol, string transactionId)
    {
        try
        {
            _logger.LogInformation("Transfer token, update transaction status start.{0}", address);
            var transactionResult = await _transferTokenProvider.GetTransactionResultAsync(chainId, transactionId);
            var times = 0;
            while ((transactionResult.Status == CommonConstant.TransactionStatePending ||
                    transactionResult.Status == CommonConstant.TransactionStateNotExisted) &&
                   times < _transferTokenOptions.CurrentValue.RetryTimes)
            {
                times++;
                await Task.Delay(_transferTokenOptions.CurrentValue.RetryDelay);

                transactionResult = await _transferTokenProvider.GetTransactionResultAsync(chainId, transactionId);
            }

            if (transactionResult.Status == CommonConstant.TransactionStateMined)
            {
                _logger.LogInformation("Transfer token, save token claim success.{0}", transactionId);
                await SaveTokenClaimRecordAsync(transactionId, TransferTokenStatus.AlreadyClaimed,
                    chainId, address, symbol);
            }
            _logger.LogInformation("Transfer token, update transaction status finished.{0}", address);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Transfer token, update transaction status error.{0}", transactionId);
        }
    }
}