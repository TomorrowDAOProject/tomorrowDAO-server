using GraphQL.Client.Abstractions;

namespace TomorrowDAOServer.Common;

public interface IGraphQlClientFactory
{
    IGraphQLClient GetClient(GraphQLClientEnum clientEnum);
}