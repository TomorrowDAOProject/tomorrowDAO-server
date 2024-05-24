using System.Collections.Generic;
using System.Configuration;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TomorrowDAOServer.EntityEventHandler.Core;
using TomorrowDAOServer.Options;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.MongoDB;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Identity;
using Volo.Abp.Identity.MongoDB;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;

namespace TomorrowDAOServer;

[DependsOn(
    typeof(AbpEventBusModule),
    typeof(TomorrowDAOServerApplicationModule),
    typeof(TomorrowDAOServerApplicationContractsModule),
    typeof(TomorrowDAOServerOrleansTestBaseModule),
    typeof(TomorrowDAOServerDomainTestModule)
)]
public class TomorrowDAOServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
        });
        
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDAOServerApplicationModule>(); });
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TomorrowDAOServerEntityEventHandlerCoreModule>(); });

        context.Services.AddSingleton(new Mock<IMongoDbContextProvider<IAuditLoggingMongoDbContext>>().Object);
        context.Services.AddSingleton<IAuditLogRepository, MongoAuditLogRepository>();
        context.Services.AddSingleton<IIdentityUserRepository, MongoIdentityUserRepository>();
        
        context.Services.AddMemoryCache();
        
        ConfigureGraphQl(context);
        ConfigureProposalTagOptions(context);
        
        base.ConfigureServices(context);
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context)
    {
        context.Services.Configure<GraphQLOptions>(o =>
        {
            o.Configuration = "http://127.0.0.1:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql";
        });
        
        context.Services.AddSingleton(new GraphQLHttpClient(
            "http://127.0.0.1:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql",
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }
    
    private void ConfigureProposalTagOptions(ServiceConfigurationContext context)
    {
        context.Services.Configure<ProposalTagOptions>(o =>
        {
            o.Mapping = new Dictionary<string, List<string>>
            {
                { "Update Organization",  new List<string>
                {
                    "AddMembers", "ChangeMember", "RemoveMembers"
                } },
                { "DAO Upgrade",  new List<string> { 
                        "UploadFileInfos",
                        "RemoveFileInfos",
                        "SetSubsistStatus",
                        "EnableHighCouncil",
                        "DisableHighCouncil",
                        "HighCouncilConfigSet",
                        "SetPermissions" 
                    } 
                },
                { "Customized Vote Model",  new List<string> { 
                        "Parliament",
                        "Association",
                        "Referendum",
                        "Customize"
                    } 
                }
            };
        });
    }
}