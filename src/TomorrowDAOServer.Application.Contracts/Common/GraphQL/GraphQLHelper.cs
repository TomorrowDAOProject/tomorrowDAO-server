using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Monitor;
using TomorrowDAOServer.Monitor.Common;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Common.GraphQL;

public interface IGraphQlHelper
{
    Task<T> QueryAsync<T>(GraphQLRequest request);
}

public class GraphQlHelper : IGraphQlHelper, ISingletonDependency
{
    private readonly IGraphQLClient _client;
    private readonly ILogger<GraphQlHelper> _logger;
    private readonly IMonitor _monitor;

    public GraphQlHelper(IGraphQLClient client, ILogger<GraphQlHelper> logger, IMonitor monitor)
    {
        _client = client;
        _logger = logger;
        _monitor = monitor;
    }

    public async Task<T> QueryAsync<T>(GraphQLRequest request)
    {
        var sw = Stopwatch.StartNew();
        
        var graphQlResponse = await _client.SendQueryAsync<T>(request);
        var isSuccess = graphQlResponse.Errors is not { Length: > 0 };

        sw.Stop();
        IDictionary<string, string> properties = new Dictionary<string, string>()
        {
            { MonitorConstant.LabelSuccess, isSuccess.ToString() }
        };
        _monitor.TrackMetric(chart: MonitorConstant.GraphQl, type: MonitorConstant.GraphQl, duration: sw.ElapsedMilliseconds,
            properties: properties);
        
        if (sw.ElapsedMilliseconds > MonitorConstant.MaxDuration)
        {
            _logger.LogInformation("Slow GraphQL Query, {0}, {1}", sw.ElapsedMilliseconds, request.Query);
        }
        
        if (isSuccess)
        {
            return graphQlResponse.Data;
        }

        _logger.LogError("query graphQL err, errors = {Errors}",
            string.Join(",", graphQlResponse.Errors.Select(e => e.Message).ToList()));
        return default;
    }
}

public class GraphQlResponseException : Exception
{
    public GraphQlResponseException(string message) : base(message)
    {
    }
}