using AElf.ExceptionHandler.ABP;
using Serilog;
using Serilog.Events;
using TomorrowDAOServer.Auth.Extension;

namespace TomorrowDAOServer.Auth;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
#if DEBUG
            .WriteTo.Async(c => c.Console())
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting TomorrowDAOServer.AuthServer.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("apollo.appsettings.json");
            builder.Host.AddAppSettingsSecretsJson()
                .UseApollo()
                .UseOrleansClient()
                .UseAutofac()
                //.UseAElfExceptionHandler()
                .UseSerilog();
            await builder.AddApplicationAsync<TomorrowDAOServerAuthServerModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0; 
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "TomorrowDAOServer.AuthServer terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}