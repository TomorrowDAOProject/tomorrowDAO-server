using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Moq;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Contract;

public partial class TransactionServiceTest
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
            ChainId = TestConstant.ChainIdAELF,
            Branches = null,
            NotLinkedBlocks = null,
            LongestChainHeight = 100,
            LongestChainHash = TransactionHash.ToHex(),
            GenesisBlockHash = TransactionHash.ToHex(),
            GenesisContractAddress = Address1,
            LastIrreversibleBlockHash = TransactionHash.ToHex(),
            LastIrreversibleBlockHeight = 100,
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
        
        httpMock.Setup(o => o.PostResponseAsync<SendTransactionOutput>(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<HttpStatusCode>(), It.IsAny<AuthenticationHeaderValue>()))
            .ReturnsAsync(() =>
            {
                return new SendTransactionOutput
                {
                    TransactionId = TransactionHash.ToHex()
                };
            });
        
        return httpMock.Object;
    }
}