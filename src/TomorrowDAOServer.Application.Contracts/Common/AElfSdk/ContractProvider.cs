using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Common.AElfSdk;

public interface IContractProvider
{
    Task<(Hash transactionId, Transaction transaction)> CreateCallTransactionAsync(string chainId,
        string contractName, string methodName, IMessage param);

    Task<(Hash transactionId, Transaction transaction)> CreateTransactionAsync(string chainId, string senderPublicKey,
        string contractName, string methodName,
        IMessage param);

    string ContractAddress(string chainId, string contractName);

    // Task SendTransactionAsync(string chainId, Transaction transaction);

    Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class;

    Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, string chainId);

    Task<string> GetTreasuryAddressAsync(string chainId, string daoId);

    Task<SendTransactionOutput> SendTransactionAsync(string chainId, Transaction transaction);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly JsonSerializerSettings _settings = JsonSettingsBuilder.New().WithAElfTypesConverters().Build();
    private readonly Dictionary<string, AElfClient> _clients = new();
    private readonly Dictionary<string, SenderAccount> _accounts = new();
    private readonly Dictionary<string, string> _emptyDict = new();
    private readonly Dictionary<string, Dictionary<string, string>> _contractAddress = new();
    private readonly SenderAccount _callTxSender;

    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly ILogger<ContractProvider> _logger;

    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .IgnoreNullValue()
        .Build();

    public ContractProvider(IOptionsMonitor<ChainOptions> chainOption, ILogger<ContractProvider> logger)
    {
        _logger = logger;
        _chainOptions = chainOption;
        InitAElfClient();
        _callTxSender = new SenderAccount(_chainOptions.CurrentValue.PrivateKeyForCallTx);
    }


    private void InitAElfClient()
    {
        if (_chainOptions.CurrentValue.ChainInfos.IsNullOrEmpty())
        {
            return;
        }

        foreach (var node in _chainOptions.CurrentValue.ChainInfos)
        {
            _clients[node.Key] = new AElfClient(node.Value.BaseUrl);
            Log.Information("init AElfClient: {ChainId}, {Node}", node.Key, node.Value.BaseUrl);
        }
    }

    private AElfClient Client(string chainId)
    {
        AssertHelper.IsTrue(_clients.ContainsKey(chainId), "AElfClient of {chainId} not found.", chainId);
        return _clients[chainId];
    }


    public string ContractAddress(string chainId, string contractName)
    {
        _ = _chainOptions.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo);
        var contractAddress = _contractAddress.GetOrAdd(chainId, _ => new Dictionary<string, string>());
        return contractAddress.GetOrAdd(contractName, name =>
        {
            var address =
                (chainInfo?.ContractAddress ?? new Dictionary<string, string>()).GetValueOrDefault(name, null);
            if (address.IsNullOrEmpty() && SystemContractName.All.Contains(name))
                address = AsyncHelper
                    .RunSync(() => Client(chainId).GetContractAddressByNameAsync(HashHelper.ComputeFrom(name)))
                    .ToBase58();

            AssertHelper.NotEmpty(address, "Address of contract {contractName} on {chainId} not exits.", name, chainId);
            return address;
        });
    }

    public Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, string chainId)
    {
        return Client(chainId).GetTransactionResultAsync(transactionId);
    }

    public async Task<string> GetTreasuryAddressAsync(string chainId, string daoId)
    {
        try
        {
            if (chainId.IsNullOrWhiteSpace() || daoId.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }
            
            var sw = Stopwatch.StartNew();
            
            var (_, transaction) = await CreateCallTransactionAsync(chainId,
                "TreasuryContractAddress", CommonConstant.TreasuryMethodGetTreasuryAccountAddress, Hash.LoadFromHex(daoId));
            var treasuryAddress =
                await CallTransactionAsync<Address>(chainId, transaction);
            
            sw.Stop();
            Log.Information("GetDAOByIdDuration: GetTreasuryAddress {0}", sw.ElapsedMilliseconds);
            
            return treasuryAddress == null ? string.Empty : treasuryAddress.ToBase58();
        }
        catch (Exception e)
        {
            Log.Error(e, "get treasury address error. daoId={0}, chainId={1}", daoId, chainId);
            return string.Empty;
        }
    }


    public async Task<(Hash transactionId, Transaction transaction)> CreateCallTransactionAsync(string chainId,
        string contractName, string methodName, IMessage param)
    {
        var pair = await CreateTransactionAsync(chainId, _callTxSender.PublicKey.ToHex(), contractName, methodName,
            param);
        pair.transaction.Signature = _callTxSender.GetSignatureWith(pair.transaction.GetHash().ToByteArray());
        return pair;
    }

    public async Task<(Hash transactionId, Transaction transaction)> CreateTransactionAsync(string chainId,
        string senderPublicKey, string contractName, string methodName,
        IMessage param)
    {
        var address = ContractAddress(chainId, contractName);
        var client = Client(chainId);
        var status = await client.GetChainStatusAsync();
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;

        // create raw transaction
        var transaction = new Transaction
        {
            From = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(senderPublicKey)),
            To = Address.FromBase58(address),
            MethodName = methodName,
            Params = param.ToByteString(),
            RefBlockNumber = height,
            RefBlockPrefix = ByteString.CopyFrom(Hash.LoadFromHex(blockHash).Value.Take(4).ToArray())
        };

        return (transaction.GetHash(), transaction);
    }

    public async Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class
    {
        var client = Client(chainId);
        // call invoke
        var rawTransactionResult = await client.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto()
        {
            RawTransaction = transaction.ToByteArray().ToHex(),
            Signature = transaction.Signature.ToHex()
        });
        if (typeof(T) == typeof(string))
        {
            return rawTransactionResult?.Substring(1, rawTransactionResult.Length - 2) as T;
        }

        return (T)JsonConvert.DeserializeObject(rawTransactionResult, typeof(T), DefaultJsonSettings);
    }

    public async Task<SendTransactionOutput> SendTransactionAsync(string chainId, Transaction transaction)
    {
        var client = Client(chainId);

        return await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });
    }
}