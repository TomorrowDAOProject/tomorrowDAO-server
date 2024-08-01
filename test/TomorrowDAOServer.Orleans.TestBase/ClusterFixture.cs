using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Options;
using AElf.Indexing.Elasticsearch.Provider;
using AElf.Types;
using AutoMapper;
using Elasticsearch.Net;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Aws;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Grains;
using TomorrowDAOServer.ThirdPart.Exchange;
using TomorrowDAOServer.User;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer;

public class ClusterFixture : IDisposable, ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        var randomPort = DateTime.UtcNow.Second * 1000 + DateTime.UtcNow.Millisecond;
        builder.Options.BaseGatewayPort = 2000 + randomPort;
        builder.Options.BaseSiloPort = 1000 + randomPort;
        builder.Options.InitialSilosCount = 1;

        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        // builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        var retryCount = 30;
        while (true)
        {
            try
            {
                Cluster.Deploy();
                break;
            }
            catch (Exception ex)
            {
                builder.Options.BaseGatewayPort++;
                builder.Options.BaseSiloPort++;
                Cluster = builder.Build();
                if (retryCount-- <= 0)
                {
                    throw;
                }
            }
        }
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }


    private class TestSiloConfigurations : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                {
                    services.AddMemoryCache();
                    services.AddDistributedMemoryCache();
                    services.AddAutoMapper(typeof(TomorrowDAOServerGrainsModule).Assembly);
                    services.AddAutoMapper(typeof(TomorrowDAOServerApplicationModule).Assembly);

                    services.AddSingleton(typeof(IDistributedCache), typeof(MemoryDistributedCache));
                    services.AddSingleton(typeof(IDistributedCache<,>), typeof(DistributedCache<,>));

                    services.AddTransient<IExchangeProvider, OkxProvider>();
                    services.AddTransient<IExchangeProvider, BinanceProvider>();
                    services.AddTransient<IExchangeProvider, CoinGeckoProvider>();
                    services.AddTransient<IUserAppService, UserAppService>();

                    // Do not modify this!!!
                    services.Configure<EsEndpointOption>(options =>
                    {
                        options.Uris = new List<string> { "http://127.0.0.1:9200" };
                    });

                    services.Configure<IndexSettingOptions>(options =>
                    {
                        options.NumberOfReplicas = 1;
                        options.NumberOfShards = 1;
                        options.Refresh = Refresh.True;
                        options.IndexPrefix = "tomorrowdaoservertest";
                    });

                    services.Configure<AbpDistributedCacheOptions>(cacheOptions =>
                    {
                        cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
                    });
                    services.OnExposing(onServiceExposingContext =>
                    {
                        //Register types for IObjectMapper<TSource, TDestination> if implements
                        onServiceExposingContext.ExposedTypes.AddRange(
                            ReflectionHelper.GetImplementedGenericTypes(
                                onServiceExposingContext.ImplementationType,
                                typeof(IObjectMapper<,>)
                            )
                        );
                    });
                    services.AddTransient(
                        typeof(IObjectMapper<>),
                        typeof(DefaultObjectMapper<>)
                    );
                    services.AddTransient(
                        typeof(IObjectMapper),
                        typeof(DefaultObjectMapper)
                    );
                    services.AddTransient(typeof(INESTRepository<,>),
                        typeof(NESTRepository<,>));
                    services.AddTransient(typeof(IEsClientProvider), typeof(DefaultEsClientProvider));
                    services.AddTransient(typeof(IAutoObjectMappingProvider),
                        typeof(AutoMapperAutoObjectMappingProvider));
                    services.AddTransient(sp => new MapperAccessor()
                    {
                        Mapper = sp.GetRequiredService<IMapper>()
                    });
                    services.AddTransient<IMapperAccessor>(provider => provider.GetRequiredService<MapperAccessor>());
                    services.AddTransient(typeof(IContractProvider), typeof(ContractProviderMock));
                    services.AddTransient(typeof(IAwsS3Client), typeof(AwsS3ClientMock));
                })
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }

    public class MapperAccessor : IMapperAccessor
    {
        public IMapper Mapper { get; set; }
    }

    public class ContractProviderMock : IContractProvider
    {
        public async Task<(Hash transactionId, Transaction transaction)> CreateCallTransactionAsync(string chainId,
            string contractName, string methodName, IMessage param)
        {
            return new (TransactionHash, new Transaction());
        }

        public async Task<(Hash transactionId, Transaction transaction)> CreateTransactionAsync(string chainId,
            string senderPublicKey, string contractName, string methodName,
            IMessage param)
        {
            return new (TransactionHash, new Transaction());;
        }

        public string ContractAddress(string chainId, string contractName)
        {
            return Address1;
        }

        public async Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class
        {
            T instance = Activator.CreateInstance<T>();
            return instance;
        }

        public Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, string chainId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTreasuryAddressAsync(string chainId, string daoId)
        {
            throw new NotImplementedException();
        }
    }

    public class AwsS3ClientMock : IAwsS3Client, ITransientDependency
    {
        public async Task<string> UpLoadFileAsync(Stream steam, string fileName)
        {
            return "UpLoadFileAsync";
        }

        public async Task<string> UpLoadBase64FileAsync(string base64Image, string fileName)
        {
            return Address1;
        }
    }
}