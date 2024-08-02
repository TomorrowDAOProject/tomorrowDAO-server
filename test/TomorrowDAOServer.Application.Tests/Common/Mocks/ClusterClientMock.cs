using System;
using System.Collections.Generic;
using Moq;
using Orleans;
using TomorrowDAOServer.Grains.Grain;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Token;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Common.Mocks;

public class ClusterClientMock
{
    public static IClusterClient MockClusterClient()
    {
        var clusterClientMock = new Mock<IClusterClient>();

        //ITokenGrain
        MockTokenGrain(clusterClientMock);
        //ITokenExchangeGrain
        MockTokenExchangeGrain(clusterClientMock);
            
        return clusterClientMock.Object;
    }

    private static void MockTokenGrain(Mock<IClusterClient> clusterClientMock)
    {
        var mock = new Mock<ITokenGrain>();
        mock.Setup(o => o.GetTokenAsync(It.IsAny<TokenGrainDto>())).ReturnsAsync(
            new GrainResultDto<TokenGrainDto>
            {
                Success = true, Data = new TokenGrainDto
                {
                    Id = "Id",
                    ChainId = ChainIdAELF,
                    Address = "Address",
                    Symbol = "ELF",
                    Decimals = 8,
                    TokenName = "AELF",
                    ImageUrl = "ImageUrl",
                    LastUpdateTime = DateTime.Now.Millisecond
                }
            });

        clusterClientMock.Setup(o => o.GetGrain<ITokenGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(mock.Object);
    }

    private static void MockTokenExchangeGrain(Mock<IClusterClient> clusterClientMock)
    {
        var mock = new Mock<ITokenExchangeGrain>();
        mock.Setup(o => o.GetAsync()).ReturnsAsync(new TokenExchangeGrainDto
        {
            LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds(), 
            ExpireTime =  DateTime.UtcNow.AddDays(1).ToUtcMilliSeconds(),
            ExchangeInfos = new Dictionary<string, TokenExchangeDto>
            {
                {"OKX", new TokenExchangeDto{ Exchange = (decimal)0.4 }}
            }
        });
        clusterClientMock.Setup(o => o.GetGrain<ITokenExchangeGrain>(It.IsAny<string>(), It.IsAny<string>())).Returns(mock.Object);
    }
}