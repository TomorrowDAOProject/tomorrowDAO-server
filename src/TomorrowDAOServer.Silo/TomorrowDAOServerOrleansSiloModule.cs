using Microsoft.Extensions.DependencyInjection;
using TomorrowDAOServer.Grains;
using TomorrowDAOServer.Grains.Grain.ApplicationHandler;
using TomorrowDAOServer.MongoDB;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.ThirdPart.Exchange;
using TomorrowDAOServer.User;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace TomorrowDAOServer.Silo;
[DependsOn(typeof(AbpAutofacModule),
    typeof(TomorrowDAOServerGrainsModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(TomorrowDAOServerMongoDbModule),
    typeof(TomorrowDAOServerApplicationModule)
)]
public class TomorrowDAOServerOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        // Configure<ChainOptions>(configuration.GetSection("Chains"));
        // Configure<ExchangeOptions>(configuration.GetSection("Exchange"));
        // Configure<CoinGeckoOptions>(configuration.GetSection("CoinGecko"));
        // Configure<AwsS3Option>(configuration.GetSection("AwsS3"));
        Configure<SecurityServerOptions>(configuration.GetSection("SecurityServer"));
        
        context.Services.AddHostedService<TomorrowDAOServerHostedService>();
        context.Services.AddTransient<IUserAppService, UserAppService>();
        context.Services.AddTransient<IExchangeProvider, OkxProvider>();
        context.Services.AddTransient<IExchangeProvider, BinanceProvider>();
        context.Services.AddTransient<IExchangeProvider, CoinGeckoProvider>();
    }
}