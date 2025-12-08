using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace TomorrowDAOServer.Common.Mocks;

/// <summary>
/// In unit testing, directly call the HttpRequestMock.MockHttpByPath to mock http requests
/// </summary>
public class HttpRequestMock
{
    private static readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private static readonly Mock<HttpMessageHandler> _mockHandler = new(MockBehavior.Strict);


    public static IHttpClientFactory MockHttpFactory()
    {
        _mockHttpClientFactory
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(new System.Net.Http.HttpClient(_mockHandler.Object) { BaseAddress = new Uri("http://test.com/") });
        return _mockHttpClientFactory.Object;
    }


    private static void MockHttpByPath(HttpMethod method, string path,
        string respData)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method && req.RequestUri.ToString().Contains(path)),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(respData, Encoding.UTF8, "application/json");
                //_outputHelper.WriteLine($"Mock Http {method} to {path}, resp={response}");
                return Task.FromResult(response);
            });
    }

    public static void MockHttpByPath(HttpMethod method, string path, object response)
    {
        MockHttpByPath(method, path, JsonConvert.SerializeObject(response));
    }
}