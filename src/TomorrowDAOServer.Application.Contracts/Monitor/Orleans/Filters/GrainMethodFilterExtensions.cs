using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;

namespace TomorrowDAOServer.Monitor.Orleans.Filters;

public static class GrainMethodFilterExtensions
{
    /// <summary>
    /// add grain method invocation monitoring
    /// </summary>
    /// <param name="clientBuilder"></param>
    public static IClientBuilder AddMethodFilter(this IClientBuilder clientBuilder, IServiceProvider serviceProvider)
    {
        return clientBuilder.ConfigureServices(services =>
        {
            MethodFilterContext.ServiceProvider = serviceProvider;
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            services.Configure<MethodCallFilterOptions>(configuration.GetSection("MethodCallFilter"));
            services.AddSingleton<IOutgoingGrainCallFilter, MethodCallFilter>();;
        });
    }
}