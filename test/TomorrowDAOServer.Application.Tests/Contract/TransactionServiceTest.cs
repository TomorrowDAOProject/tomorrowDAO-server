using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Client;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract.Dto;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Contract;

public partial class TransactionServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITransactionService _transactionService;
    private readonly IAElfClientProvider _aElfClientProvider;

    public TransactionServiceTest(ITestOutputHelper output) : base(output)
    {
        _aElfClientProvider = ServiceProvider.GetRequiredService<IAElfClientProvider>();
        _transactionService = ServiceProvider.GetRequiredService<ITransactionService>();

        _aElfClientProvider.GetClient(ChainIdAELF);
        
        var type = _aElfClientProvider.GetType();
        FieldInfo? aelfCilientField = type.GetField("_clientDic", BindingFlags.NonPublic | BindingFlags.Instance);
        var dictionary = (ConcurrentDictionary<string, AElfClient>)aelfCilientField.GetValue(_aElfClientProvider);
        foreach (var aElfClient in dictionary)
        {
            FieldInfo? httpServiceField =
                typeof(AElfClient).GetField("_httpService", BindingFlags.NonPublic | BindingFlags.Instance);
            httpServiceField.SetValue(aElfClient.Value, MockHttpService());
        }
    }

    [Fact]
    public async Task CallTransactionAsyncTest()
    {
        var currentMinerPubkeyList =
            await _transactionService.CallTransactionAsync<string>(ChainIdAELF, PrivateKey2,
                Address1, ContractConstants.GetCurrentMinerPubkeyList);
        currentMinerPubkeyList.ShouldNotBeNull();
        currentMinerPubkeyList.ShouldBe("\"2Svg2WHtfgd9L2AMTj1gkMatByRgLk88kwyX8H7zL6pejKfL32\"");
    }

    [Fact]
    public async Task SendTransactionAsyncTest()
    {
        var transactionId = await _transactionService.SendTransactionAsync(ChainIdAELF, PrivateKey2,
            Address1, ContractConstants.GetCurrentMinerPubkeyList);
        transactionId.ShouldNotBeNull();
        transactionId.ShouldBe(TransactionHash.ToHex());
    }

    [Fact]
    public async Task GetTransactionByIdTest()
    {
        var transactionResultDto = await _transactionService.GetTransactionById(ChainIdAELF, TransactionHash.ToHex());
        transactionResultDto.ShouldNotBeNull();
        transactionResultDto.TransactionId.ShouldBe(TransactionHash.ToHex());
    }
}