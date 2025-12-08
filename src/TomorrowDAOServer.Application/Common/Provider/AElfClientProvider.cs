using System.Collections.Concurrent;
using AElf.Client;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Common.Provider;

public interface IAElfClientProvider
{
    AElfClient GetClient(string chainName);
}

public class AElfClientProvider : IAElfClientProvider, ISingletonDependency
{
    private readonly ApiOption _apiOption;
    private readonly ConcurrentDictionary<string, AElfClient> _clientDic;
    
    public AElfClientProvider(IOptionsSnapshot<ApiOption> apiOptions)
    {
        _apiOption = apiOptions.Value;
        _clientDic = new ConcurrentDictionary<string, AElfClient>();
    }
    
    public AElfClient GetClient(string chainName)
    {
        if (_clientDic.TryGetValue(chainName, out var client))
        {
            return client;
        }
    
        _clientDic[chainName] = new AElfClient(_apiOption.ChainNodeApis[chainName]);
        return _clientDic[chainName];
    }
}