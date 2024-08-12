using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.ThirdPart.Exchange;
using TomorrowDAOServer.Token;
using Volo.Abp.Caching;
using Xunit;

namespace TomorrowDAOServer.Application.Contracts.Tests.ThirdPart;

public class AwakenProviderTest
{
    private readonly IOptionsMonitor<ExchangeOptions> _exchangeOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IDistributedCache<TokenExchangeDto> _exchangeCache;
    private readonly IExchangeProvider _provider;

    public AwakenProviderTest()
    {
        _exchangeOptions = Substitute.For<IOptionsMonitor<ExchangeOptions>>();
        _httpProvider = Substitute.For<IHttpProvider>();
        _exchangeCache = Substitute.For<IDistributedCache<TokenExchangeDto>>();
        _provider = new AwakenProvider(_exchangeOptions, _httpProvider, _exchangeCache);
    }

    [Fact]
    public async Task Name_Test()
    {
        var result = _provider.Name();
        result.ShouldBe(ExchangeProviderName.Awaken);
    }
    
    [Fact]
    public async Task LatestAsync_Test()
    {
        _httpProvider.InvokeAsync<CommonResponseDto<string>>(Arg.Any<string>(), Arg.Any<ApiInfo>(),
                Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, string>>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(),
                Arg.Any<JsonSerializerSettings>(), Arg.Any<int?>(),
                Arg.Any<bool>(), Arg.Any<bool>()
                )
            .Returns(new CommonResponseDto<string>
            {
                Data = "0.4", Code = "20000", Message = ""
            });
        _exchangeOptions.CurrentValue.Returns(new ExchangeOptions
        {
            Awaken = new AwakenOptions { BaseUrl = "url" }
        });
        var result = await _provider.LatestAsync("ELF", "USD");
        result.ShouldNotBeNull();
        result.Exchange.ShouldBe((decimal)0.4);
    }

    [Fact]
    public async Task HistoryAsync_Test()
    {
        var result = await _provider.HistoryAsync("ELF", "USD", 0);
        result.ShouldNotBeNull();
    }
}