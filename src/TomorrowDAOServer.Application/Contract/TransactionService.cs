using System.Threading.Tasks;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common.Provider;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Contract;

public interface ITransactionService
{
    Task<T> CallTransactionAsync<T>(string chainId, string privateKey, string toAddress, string methodName) where T : class;
    Task<T> CallTransactionAsync<T>(string chainId, string privateKey, string toAddress, string methodName, IMessage txParam) where T : class;
    Task<string> SendTransactionAsync(string chainId, string privateKey, string toAddress, string methodName);
    Task<string> SendTransactionAsync(string chainId, string privateKey, string toAddress, string methodName, IMessage txParam);
    Task<TransactionResultDto> GetTransactionById(string chainId, string txId);
}

public class TransactionService : ITransactionService, ITransientDependency
{
    private readonly IAElfClientProvider _clientProvider;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(IAElfClientProvider clientProvider, ILogger<TransactionService> logger)
    {
        _clientProvider = clientProvider;
        _logger = logger;
    }

    public Task<T> CallTransactionAsync<T>(string chainId, string privateKey, string toAddress, string methodName) where T : class
    {
        return CallTransactionAsync<T>(chainId, privateKey, toAddress, methodName, new Empty());
    }

    public Task<string> SendTransactionAsync(string chainId, string privateKey, string toAddress, string methodName)
    {
        return SendTransactionAsync(chainId, privateKey, toAddress, methodName, new Empty());
    }
    
    public async Task<T> CallTransactionAsync<T>(string chainId, string privateKey, string toAddress, string methodName, IMessage txParam) where T : class
    {
        var (client,transaction) = await GetTransaction(chainId, privateKey, toAddress, methodName, txParam);

        var result = await client.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto
        {
            RawTransaction = transaction.ToByteArray().ToHex(),
            Signature = transaction.Signature.ToHex()
        });
        if (typeof(T) == typeof(string))
        {
            return result as T;
        }

        return (T)JsonConvert.DeserializeObject(result ?? string.Empty, typeof(T));
    }

    public async Task<string> SendTransactionAsync(string chainId, string privateKey, string toAddress, string methodName, IMessage txParam)
    {
        Log.Information("SendTransactionAsyncBegin ToAddress={toAddress} chainId={chainId} MethodName={methodName}", toAddress, chainId, methodName);
        var (client,transaction) = await GetTransaction(chainId, privateKey, toAddress, methodName, txParam);

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });
        Log.Information("SendTransactionAsyncEnd ToAddress={toAddress} chainId={chainId} MethodName={methodName}, TransactionId={transactionId}", toAddress, chainId, methodName, result?.TransactionId ?? string.Empty);
        return result?.TransactionId ?? string.Empty;
    }

    public async Task<TransactionResultDto> GetTransactionById(string chainId, string txId)
    {
        var client = _clientProvider.GetClient(chainId);
        return await client.GetTransactionResultAsync(txId);
    }

    private async Task<(AElfClient client, Transaction transaction)> GetTransaction(string chainId, string privateKey, string toAddress, string methodName, IMessage txParam)
    {
        var client = _clientProvider.GetClient(chainId);
        var ownerAddress = client.GetAddressFromPrivateKey(privateKey);
        var transaction = await client.GenerateTransactionAsync(ownerAddress, toAddress, methodName, txParam);
        var signedTransaction = client.SignTransaction(privateKey, transaction);
        return (client, signedTransaction);
    }
}