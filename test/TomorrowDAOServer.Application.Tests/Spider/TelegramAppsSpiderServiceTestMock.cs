using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Telegram.Dto;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Spider;

public partial class TelegramAppsSpiderServiceTest
{
    private IOptionsMonitor<TelegramOptions> MockTelegramOptions()
    {
        var mock = new Mock<IOptionsMonitor<TelegramOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(new TelegramOptions
        {
            AllowedCrawlUsers = new HashSet<string>() { Address2 }
        });

        return mock.Object;
    }
    
    private void MockTelegramUrlRequest()
    {
        string dtoStr =
                "{\"Data\":[{\"Id\":12,\"Attributes\":{\"Title\":\"Favorite Stickers Bot\",\"Description\":\"I'm the emoji and sticker wizard! Transform photos, videos, and GIFs into cool stickers in a snap.\",\"Url\":\"https://t.me/fStikBot/\",\"Path\":\"favoritestickersbot\",\"CreatedAt\":\"2023-08-07T17:46:49.667Z\",\"UpdatedAt\":\"2024-06-17T18:45:54.518Z\",\"PublishedAt\":\"2023-08-07T17:47:21.940Z\",\"Locale\":\"en\",\"EditorsChoice\":null,\"WebappUrl\":null,\"CommunityUrl\":null,\"Long_description\":null,\"StartParam\":null,\"Ecosystem\":null,\"Ios\":false,\"AnalyticsId\":null,\"Screenshots\":{\"Data\":null}}}],\"Meta\":{\"Pagination\":{\"Start\":0,\"Limit\":1000,\"Total\":1}}}";
        var dto = JsonConvert.DeserializeObject<TelegramAppDetailDto>(dtoStr);
        HttpRequestMock.MockHttpByPath(HttpMethod.Get, _url, dto);
    }
}