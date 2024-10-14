using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Serialization.Invocation;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Token;
using TomorrowDAOServer.Monitor.Orleans.Filters;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Application.Contracts.Tests.Monitor.Filters;

public partial class MethodCallFilterTest : TomorrowDaoServerApplicationContractsTestsBase
{
    private readonly MethodCallFilter _methodCallFilter;

    public MethodCallFilterTest(ITestOutputHelper output) : base(output)
    {
        MethodFilterContext.ServiceProvider = Application.ServiceProvider;
        _methodCallFilter = ServiceProvider.GetRequiredService<MethodCallFilter>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockMethodCallFilterOptions());
    }

    [Fact]
    private async Task InvokeTest()
    {
        await _methodCallFilter.Invoke(new TestOutgoingGrainCall
        {
            Grain = null,
            Method = typeof(TokenGrain).GetMethod("GetTokenInfoAsync"),
            InterfaceMethod = typeof(ITokenGrain).GetMethod("GetTokenInfoAsync"),
            Arguments = new object[0],
            Result = new TokenInfoDto()
        });

        await _methodCallFilter.Invoke(new TestOutgoingGrainCall
        {
            Grain = null,
            Method = typeof(TokenGrain).GetMethod("GetTokenInfoAsync"),
            InterfaceMethod = typeof(ITokenGrain).GetMethod("GetTokenInfoAsync"),
            Arguments = new object[0],
            Result = new TokenInfoDto()
        });

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _methodCallFilter.Invoke(new TestOutgoingGrainCall
            {
                Grain = null,
                Method = typeof(TokenExchangeGrain).GetMethod("GetAsync"),
                InterfaceMethod = typeof(ITokenExchangeGrain).GetMethod("GetAsync"),
                Arguments = new object[0],
                Result = new TokenInfoDto()
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldContain("Grain Exception Test");
    }

    internal class TestOutgoingGrainCall : IOutgoingGrainCallContext
    {
        public Task Invoke()
        {
            if (Method.Name != "GetTokenInfoAsync")
            {
                throw new UserFriendlyException("Grain Exception Test");
            }

            return Task.CompletedTask;
        }

        public IInvokable Request { get; }
        object IGrainCallContext.Grain { get; }
        public GrainId? SourceId { get; }
        public GrainId TargetId { get; }
        public GrainInterfaceType InterfaceType { get; }
        public string InterfaceName { get; }
        public string MethodName { get; }
        public IAddressable Grain { get; set; }
        public MethodInfo Method { get; set; }
        public MethodInfo InterfaceMethod { get; set; }
        public object[] Arguments { get; set; }
        public object Result { get; set; }
        public Response Response { get; set; }
        public IGrainContext SourceContext { get; }
    }
}