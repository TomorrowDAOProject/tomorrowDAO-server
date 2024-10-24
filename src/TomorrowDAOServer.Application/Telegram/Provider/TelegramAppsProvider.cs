using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Telegram.Dto;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Telegram.Provider;

public interface ITelegramAppsProvider
{
    Task AddOrUpdateAsync(TelegramAppIndex telegramAppIndex);
    Task BulkAddOrUpdateAsync(List<TelegramAppIndex> telegramAppIndices);
    Task<Tuple<long, List<TelegramAppIndex>>> GetTelegramAppsAsync(QueryTelegramAppsInput input);
    Task<List<TelegramAppIndex>> GetAllAsync();
    Task<List<TelegramAppIndex>> GetAllDisplayAsync(List<string> aliases);
    Task<Tuple<long, List<TelegramAppIndex>>> GetByCategoryAsync(TelegramAppCategory category, int skipCount, int maxResultCount);
    Task<List<TelegramAppIndex>> SearchAppAsync(string title);
}

public class TelegramAppsProvider : ITelegramAppsProvider, ISingletonDependency
{
    private readonly ILogger<TelegramAppsProvider> _logger;
    private readonly INESTRepository<TelegramAppIndex, string> _telegramAppIndexRepository;

    public TelegramAppsProvider(ILogger<TelegramAppsProvider> logger,
        INESTRepository<TelegramAppIndex, string> telegramAppIndexRepository)
    {
        _logger = logger;
        _telegramAppIndexRepository = telegramAppIndexRepository;
    }

    public async Task AddOrUpdateAsync(TelegramAppIndex telegramAppIndex)
    {
        await _telegramAppIndexRepository.AddOrUpdateAsync(telegramAppIndex);
    }

    public async Task BulkAddOrUpdateAsync(List<TelegramAppIndex> telegramAppIndices)
    {
        if (telegramAppIndices == null || telegramAppIndices.IsNullOrEmpty())
        {
            return;
        }
        await _telegramAppIndexRepository.BulkAddOrUpdateAsync(telegramAppIndices);
    }

    public async Task<Tuple<long, List<TelegramAppIndex>>> GetTelegramAppsAsync(QueryTelegramAppsInput input)
    {
        if (input == null)
        {
            return new Tuple<long, List<TelegramAppIndex>>(0, new List<TelegramAppIndex>());
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>();

        if (!input.Ids.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(input.Ids)));
        }

        if (!input.Names.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Title).Terms(input.Names)));
        }

        if (!input.Aliases.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Alias).Terms(input.Aliases)));
        }

        if (input.SourceType != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.SourceType).Value(input.SourceType)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _telegramAppIndexRepository.GetListAsync(Filter);
    }

    public async Task<List<TelegramAppIndex>> GetAllAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>();
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    public async Task<List<TelegramAppIndex>> GetAllDisplayAsync(List<string> aliases)
    {
        var mustNotQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>();
        if (!aliases.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Terms(t => t.Field(f => f.Alias).Terms(aliases)));
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Exists(i => i.Field(f => f.Url)),
            q => q.Exists(i => i.Field(f => f.LongDescription)),
            q => q.Exists(i => i.Field(f => f.Screenshots)),
            q => q.Exists(i => i.Field(f => f.Categories)),
            q => q.Term(i => i.Field(f => f.SourceType).Value(SourceType.Telegram)) 
        };
        
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b
                .MustNot(mustNotQuery).Must(mustQuery)
        );

        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    public async Task<Tuple<long, List<TelegramAppIndex>>> GetByCategoryAsync(TelegramAppCategory category, int skipCount, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Terms(t => t.Field(f => f.Categories).Terms(category)),
            q => q.Exists(i => i.Field(f => f.Url)),
            q => q.Exists(i => i.Field(f => f.LongDescription)),
            q => q.Exists(i => i.Field(f => f.Screenshots))
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _telegramAppIndexRepository.GetListAsync(Filter, skip: skipCount, limit: maxResultCount);
    }

    public async Task<List<TelegramAppIndex>> SearchAppAsync(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return new List<TelegramAppIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Match(m => m.Field(f => f.Title).Query(title))
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }
}