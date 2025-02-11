using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.ChainFm.Index;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.ChainFm.Provider;

public interface ISmartMoneyProvider
{
    Task BulkAddOrUpdateAsync(List<SmartMoneyIndex> smartMoneyIndices);
}

public class SmartMoneyProvider : ISmartMoneyProvider, ISingletonDependency
{
    private readonly ILogger<ChainFmChannelProvider> _logger;
    private readonly INESTRepository<SmartMoneyIndex, string> _smartMoneyIndexRepository;

    public SmartMoneyProvider(ILogger<ChainFmChannelProvider> logger,
        INESTRepository<SmartMoneyIndex, string> smartMoneyIndexRepository)
    {
        _logger = logger;
        _smartMoneyIndexRepository = smartMoneyIndexRepository;
    }


    public async Task BulkAddOrUpdateAsync(List<SmartMoneyIndex> smartMoneyIndices)
    {
        if (smartMoneyIndices.IsNullOrEmpty())
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var smartMoneyIndex in smartMoneyIndices)
        {
            if (smartMoneyIndex.CreateTime == default)
            {
                smartMoneyIndex.CreateTime = now;
            }
            smartMoneyIndex.UpdateTime = now;
        }

        await _smartMoneyIndexRepository.BulkAddOrUpdateAsync(smartMoneyIndices);
    }
}