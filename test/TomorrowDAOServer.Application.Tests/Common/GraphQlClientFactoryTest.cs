using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using TomorrowDAOServer.Options;
using Xunit;

namespace TomorrowDAOServer.Common;

public class GraphQlClientFactoryTest
{
    private static readonly IOptionsSnapshot<GraphQLOptions> GraphQlClientOptions = Substitute.For<IOptionsSnapshot<GraphQLOptions>>();
    private readonly GraphQlClientFactory _graphQlClientFactory;

    public GraphQlClientFactoryTest()
    {
        GraphQlClientOptions.Value.Returns(new GraphQLOptions
        {
            Configuration = " http://127.0.0.1:8107/Configuration",
            ModuleConfiguration = "http://127.0.0.1:8107/ModuleConfiguration"
        });
        _graphQlClientFactory = new GraphQlClientFactory(GraphQlClientOptions);
    }

    [Fact]
    public  void GetClient_Test()
    {
        var graphQlClient = _graphQlClientFactory.GetClient(GraphQLClientEnum.ModuleClient);
        graphQlClient.ShouldNotBeNull();
        
        graphQlClient = _graphQlClientFactory.GetClient(GraphQLClientEnum.TomorrowDAOClient);
        graphQlClient.ShouldNotBeNull();
        
        graphQlClient = _graphQlClientFactory.GetClient(GraphQLClientEnum.ModuleClient);
        graphQlClient.ShouldNotBeNull();
    }
}