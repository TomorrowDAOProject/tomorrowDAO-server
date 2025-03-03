using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using Newtonsoft.Json;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Referral.Dto;

namespace TomorrowDAOServer.Providers;

public partial class PortkeyProviderTest
{
    private IHttpProvider MockHttpProvider()
    {
        var mock = new Mock<IHttpProvider>();

        mock.Setup(o => o.InvokeAsync<ShortLinkResponse>(It.IsAny<string>(), It.IsAny<ApiInfo>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<Dictionary<string, string>>()
            , It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JsonSerializerSettings>(),
            It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new ShortLinkResponse
        {
            CaHash = "CaHash",
            ProjectCode = "ProjectCode",
            InviteCode = "InviteCode",
            ShortLinkCode = "ShortLinkCode"
        });

        mock.Setup(o => o.InvokeAsync<ReferralCodeResponse>(It.IsAny<HttpMethod>(), It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<Dictionary<string, string>>()
            , It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JsonSerializerSettings>(),
            It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(new ReferralCodeResponse
        {
            Data = new List<ReferralCodeInfo>()
            {
                new ReferralCodeInfo
                {
                    CaHash = "CaHash",
                    InviteCode = "InviteCode"
                }
            }
        });

        mock.Setup(o => o.InvokeAsync<GuardianIdentifiersResponse>(It.IsAny<HttpMethod>(), It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<Dictionary<string, string>>()
            , It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JsonSerializerSettings>(),
            It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((HttpMethod method, string url,
            Dictionary<string, string> pathParams,
            Dictionary<string, string> param,
            string body,
            Dictionary<string, string> header, JsonSerializerSettings settings, int? timeout,
            bool withInfoLog, bool withDebugLog) =>
        {
            if (url.Contains("ThrowException"))
            {
                throw new SystemException("Exception");
            }
            
            return new GuardianIdentifiersResponse
            {
                GuardianList = new GuardianIdentifierList
                {
                    Guardians = new List<Guardian>()
                    {
                        new Guardian
                        {
                            IdentifierHash = "IdentifierHash",
                            GuardianIdentifier = "GuardianIdentifier"
                        }
                    }
                }
            };
        });

        return mock.Object;
    }


    private void MockShortLinkHttpRequest()
    {
        HttpRequestMock.MockHttpByPath(ReferralApi.ShortLink.Method, ReferralApi.ShortLink.Path, new ShortLinkResponse
        {
            CaHash = "CaHash",
            ProjectCode = "ProjectCode",
            InviteCode = "InviteCode",
            ShortLinkCode = "ShortLinkCode"
        });
    }
}