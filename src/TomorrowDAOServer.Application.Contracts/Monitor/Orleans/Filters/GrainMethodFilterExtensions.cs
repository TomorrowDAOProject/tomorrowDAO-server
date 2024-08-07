using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace TomorrowDAOServer.Monitor.Orleans.Filters;

public static class GrainMethodFilterExtensions
{
    /// <summary>
    /// add grain method invocation monitoring
    /// </summary>
    /// <param name="clientBuilder"></param>
    public static IClientBuilder AddMethodFilter(this IClientBuilder clientBuilder, IServiceProvider serviceProvider)
    {
        return clientBuilder.ConfigureServices((context, services) =>
        {
            MethodFilterContext.ServiceProvider = serviceProvider;
            services.Configure<MethodCallFilterOptions>(context.Configuration.GetSection("MethodCallFilter"));
            services.AddSingleton<IOutgoingGrainCallFilter, MethodCallFilter>();;
        });
    }
}