using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common.AElfSdk;

public partial class ContractProviderTest
{
    public static readonly JsonSerializerSettings DefaultJsonSettings = JsonSettingsBuilder.New()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .IgnoreNullValue()
        .Build();
    
    private AElfClient MockAElfClient()
    {
        var mock = new Mock<AElfClient>() { CallBase = true };
        var httpMock = new Mock<IHttpService>();

        // mock.Setup(o => o.GetTransactionResultAsync(It.IsAny<string>())).ReturnsAsync(new TransactionResultDto
        // {
        //     TransactionId = TransactionHash.ToHex(),
        //     Status = "Mined",
        //     Logs = new LogEventDto[]
        //     {
        //     },
        //     Bloom = null,
        //     BlockNumber = 100,
        //     BlockHash = null,
        //     Transaction = null,
        //     ReturnValue = null,
        //     Error = null
        // });

        httpMock.Setup(o =>
            o.GetResponseAsync<TransactionResultDto>(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HttpStatusCode>())).ReturnsAsync(new TransactionResultDto
        {
            TransactionId = TransactionHash.ToHex(),
            Status = "Mined",
            Logs = new LogEventDto[]
            {
            },
            Bloom = null,
            BlockNumber = 100,
            BlockHash = null,
            Transaction = null,
            ReturnValue = null,
            Error = null
        });


        //mock.Protected().SetupGet<IHttpService>("_httpService").Returns(httpMock.Object);

        var field = typeof(AElfClient).GetField("_httpService", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockAElfClient = mock.Object;
        field.SetValue(mockAElfClient, httpMock.Object);

        return mockAElfClient;
    }

    private IHttpService MockHttpService()
    {
        var httpMock = new Mock<IHttpService>();

        httpMock.Setup(o =>
            o.GetResponseAsync<TransactionResultDto>(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HttpStatusCode>())).ReturnsAsync(new TransactionResultDto
        {
            TransactionId = TransactionHash.ToHex(),
            Status = "Mined",
            Logs = new LogEventDto[]
            {
            },
            Bloom = null,
            BlockNumber = 100,
            BlockHash = null,
            Transaction = null,
            ReturnValue = null,
            Error = null
        });

        httpMock.Setup(o => o.GetResponseAsync<ChainStatusDto>(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<HttpStatusCode>())).ReturnsAsync(new ChainStatusDto
        {
            ChainId = ChainIdAELF,
            Branches = null,
            NotLinkedBlocks = null,
            LongestChainHeight = 0,
            LongestChainHash = null,
            GenesisBlockHash = null,
            GenesisContractAddress = null,
            LastIrreversibleBlockHash = null,
            LastIrreversibleBlockHeight = 0,
            BestChainHash = TransactionHash.ToHex(),
            BestChainHeight = 100
        });

        httpMock.Setup(o => o.PostResponseAsync<string>(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<HttpStatusCode>(), It.IsAny<AuthenticationHeaderValue>()))
            .ReturnsAsync(() =>
            {
                return JsonConvert.SerializeObject(Address.FromBase58(TreasuryAddress), DefaultJsonSettings);
            });

        return httpMock.Object;
    }
}