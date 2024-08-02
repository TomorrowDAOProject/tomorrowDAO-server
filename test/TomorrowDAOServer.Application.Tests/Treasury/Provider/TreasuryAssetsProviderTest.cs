using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Mocks;
using TomorrowDAOServer.Treasury.Dto;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Treasury.Provider;

public class TreasuryAssetsProviderTest : TomorrowDaoServerApplicationTestBase
{ 
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ITreasuryAssetsProvider _provider;

    public TreasuryAssetsProviderTest(ITestOutputHelper output) : base(output)
    {
        _provider = ServiceProvider.GetRequiredService<ITreasuryAssetsProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GraphQLClientMock.MockTreasuryProviderGraphQL());
        
    }

    [Fact]
    public async void GetTreasuryAssetsAsync_Test()
    {
        var result = await _provider.GetTreasuryAssetsAsync(new GetTreasuryAssetsInput());
        result.ShouldNotBeNull();
    }
    
    [Fact]
    public async void GetAllTreasuryAssetsAsync_Test()
    {
        var result = await _provider.GetAllTreasuryAssetsAsync(new GetAllTreasuryAssetsInput());
        result.ShouldNotBeNull();
    }

    [Fact]
    public async void GetTreasuryRecordListAsync_Test()
    {
        var result = await _provider.GetTreasuryRecordListAsync(new GetTreasuryRecordListInput());
        result.ShouldNotBeNull();
    }
}