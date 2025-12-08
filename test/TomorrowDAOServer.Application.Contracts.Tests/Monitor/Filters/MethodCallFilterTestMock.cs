using Microsoft.Extensions.Options;
using Moq;
using TomorrowDAOServer.Monitor.Orleans.Filters;

namespace TomorrowDAOServer.Application.Contracts.Tests.Monitor.Filters;

public partial class MethodCallFilterTest
{
    private bool _isEnabled = false;
    private IOptionsMonitor<MethodCallFilterOptions> MockMethodCallFilterOptions()
    {
        var mock = new Mock<IOptionsMonitor<MethodCallFilterOptions>>();

        mock.Setup(o => o.CurrentValue).Returns(() =>
        {
            _isEnabled = !_isEnabled;
            return new MethodCallFilterOptions
            {
                IsEnabled = _isEnabled,
                SkippedMethods = new HashSet<string>()
                    { "TomorrowDAOServer.Grains.Grain.Token.TokenGrain.GetTokenInfoAsync" }
            };
        });

        return mock.Object;
    }
}