using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Dto;
using TomorrowDAOServer.Treasury.Dto;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Treasury;

public partial class TreasuryAssetsServiceTest
{
    private ITokenService MockTokenService()
    {
        var mock = new Mock<ITokenService>();

        mock.Setup(o => o.GetTokenAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new TokenGrainDto
        {
            Id = "Id",
            ChainId = ChainIdAELF,
            Address = "Address",
            Symbol = ELF,
            Decimals = 8,
            TokenName = "ELF Symbol",
            ImageUrl = "ImageUrl",
            LastUpdateTime = DateTime.Now.Millisecond
        });

        mock.Setup(o => o.GetTokenPriceAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new TokenPriceDto
        {
            BaseCoin = ELF,
            QuoteCoin = CommonConstant.USD,
            Price = new decimal(ElfPrice)
        });
        
        return mock.Object;
    }
}