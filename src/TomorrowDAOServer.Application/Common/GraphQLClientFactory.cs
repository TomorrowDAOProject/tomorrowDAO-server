using System.Collections.Concurrent;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Common
{
    public class GraphQlClientFactory : IGraphQlClientFactory, ISingletonDependency
    {
        private readonly GraphQLOptions _graphQlClientOptions;
        private readonly ConcurrentDictionary<string, IGraphQLClient> _clientDic;
        private static readonly object LockObject = new();
        public GraphQlClientFactory(IOptionsSnapshot<GraphQLOptions> graphQlClientOptions)
        {
            _graphQlClientOptions = graphQlClientOptions.Value;
            _clientDic = new ConcurrentDictionary<string, IGraphQLClient>();
        }

        public IGraphQLClient GetClient(GraphQLClientEnum clientEnum)
        {
            var clientName = clientEnum.ToString();
            
            if (_clientDic.TryGetValue(clientName, out var client))
            {
                return client;
            }

            lock (LockObject)
            {
                if (_clientDic.TryGetValue(clientName, out client))
                {
                    return client;
                }

                client = clientEnum == GraphQLClientEnum.ModuleClient
                    ? new GraphQLHttpClient(_graphQlClientOptions.ModuleConfiguration,
                        new NewtonsoftJsonSerializer())
                    : new GraphQLHttpClient(_graphQlClientOptions.Configuration,
                        new NewtonsoftJsonSerializer());
                _clientDic[clientName] = client;
            }
            return client;
        }
    }
}