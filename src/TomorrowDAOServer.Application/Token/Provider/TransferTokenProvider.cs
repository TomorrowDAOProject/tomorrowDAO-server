using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Token.Provider;

public interface ITransferTokenProvider
{
    Task<GetBalanceOutput> GetBalanceAsync(string chainId, string symbol, string address);

    Task<SendTransactionOutput> TransferTokenAsync(SenderAccount account, string chainId, string symbol, string address,
        long amount);

    Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId);
}

public class TransferTokenProvider : ITransferTokenProvider, ISingletonDependency
{
    private readonly ILogger<TransferTokenProvider> _logger;
    private readonly IContractProvider _contractProvider;

    private readonly SenderAccount _senderAccount;

    public TransferTokenProvider(ILogger<TransferTokenProvider> logger, IContractProvider contractProvider)
    {
        _logger = logger;
        _contractProvider = contractProvider;
    }

    public async Task<GetBalanceOutput> GetBalanceAsync(string chainId, string symbol, string address)
    {
        try
        {
            if (chainId.IsNullOrWhiteSpace() || symbol.IsNullOrWhiteSpace() || address.IsNullOrWhiteSpace())
            {
                return new GetBalanceOutput();
            }

            var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
                SystemContractName.TokenContract, CommonConstant.TokenMethodGetBalance,
                new GetBalanceInput
                {
                    Symbol = symbol,
                    Owner = Address.FromBase58(address)
                });
            var getBalanceOutput =
                await _contractProvider.CallTransactionAsync<GetBalanceOutput>(chainId, transaction);

            return getBalanceOutput ?? new GetBalanceOutput();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Call {0} error. {1},{2},{3}",
                CommonConstant.TokenMethodGetBalance, chainId, symbol, address);
            return new GetBalanceOutput();
        }
    }

    public async Task<SendTransactionOutput> TransferTokenAsync(SenderAccount account, string chainId, string symbol,
        string address, long amount)
    {
        if (chainId.IsNullOrWhiteSpace() || symbol.IsNullOrWhiteSpace() || address.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        var (_, transaction) = await _contractProvider.CreateTransactionAsync(chainId, account.PublicKey.ToHex(),
            SystemContractName.TokenContract, CommonConstant.TokenMethodTransfer,
            new TransferInput
            {
                To = Address.FromBase58(address),
                Symbol = symbol,
                Amount = amount
            });

        transaction.Signature = account.GetSignatureWith(transaction.GetHash().ToByteArray());

        var sendTransactionOutput = await _contractProvider.SendTransactionAsync(chainId, transaction);

        return sendTransactionOutput;
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        return await _contractProvider.QueryTransactionResultAsync(transactionId, chainId);
    }
}