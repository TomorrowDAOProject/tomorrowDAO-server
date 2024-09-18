using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TomorrowDAOServer.Extension;
using Serilog;
using Serilog.Events;
using TomorrowDAOServer.Hubs;

namespace TomorrowDAOServer
{
    public class Program
    {
        public async static Task<int> Main(string[] args)
        {
            System.Threading.ThreadPool.SetMinThreads(300, 300);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting TomorrowDAOServer.HttpApi.Host");

                var builder = WebApplication.CreateBuilder(args);
                builder.Configuration.AddJsonFile("apollo.appsettings.json");
                builder.Host.AddAppSettingsSecretsJson()
                    .UseApollo()
                    .UseAutofac()
                    .UseSerilog();

                await builder.AddApplicationAsync<TomorrowDAOServerHttpApiHostModule>();
                builder.Services.AddSignalR();
                var app = builder.Build();
                app.MapHub<PointsHub>("api/app/ranking/points");
                app.MapHub<UserBalanceHub>("api/app/user/balance");
                await app.InitializeApplicationAsync();
                await app.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}