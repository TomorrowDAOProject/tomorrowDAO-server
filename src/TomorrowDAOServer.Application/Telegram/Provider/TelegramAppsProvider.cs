using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
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
    Task<Tuple<long, List<TelegramAppIndex>>> GetTelegramAppsAsync(QueryTelegramAppsInput input, bool excludeDao = false);
    Task< List<TelegramAppIndex>> GetAllTelegramAppsAsync(QueryTelegramAppsInput input);
    Task<List<TelegramAppIndex>> GetNeedLoadDetailAsync();
    Task<List<TelegramAppIndex>> GetNeedSetCategoryAsync();
    Task<long> CountAllDisplayAsync();
    Task<List<TelegramAppIndex>> GetAllDisplayAsync(List<string> excludeAliases, int count, List<TelegramAppCategory> categories = null);
    Task<Tuple<long, List<TelegramAppIndex>>> GetByCategoryAsync(TelegramAppCategory category, int skipCount, int maxResultCount);
    Task<List<TelegramAppIndex>> SearchAppAsync(string title);
    Task<TelegramAppIndex> GetLatestCreatedAsync();
    Task<Tuple<long, List<TelegramAppIndex>>> GetByTimePeriodAsync(DateTime start, DateTime end, int skipCount, int maxResultCount);
    Task<List<TelegramAppIndex>> GetAllByTimePeriodAsync(DateTime start, DateTime end);
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

    public async Task<Tuple<long, List<TelegramAppIndex>>> GetTelegramAppsAsync(QueryTelegramAppsInput input, bool excludeDao = false)
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

        if (excludeDao)
        {
            mustQuery.Add(q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await _telegramAppIndexRepository.GetListAsync(Filter);
    }

    public async Task<List<TelegramAppIndex>> GetAllTelegramAppsAsync(QueryTelegramAppsInput input)
    {
        if (input == null)
        {
            return new List<TelegramAppIndex>();
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

        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    public async Task<List<TelegramAppIndex>> GetNeedLoadDetailAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => !q.Exists(i => i.Field(f => f.Url)),
            q => q.Term(i => i.Field(f => f.SourceType).Value(SourceType.Telegram))
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    public async Task<List<TelegramAppIndex>> GetNeedSetCategoryAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => !q.Exists(i => i.Field(f => f.Categories)),
            q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao))
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    public async Task<long> CountAllDisplayAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Exists(i => i.Field(f => f.Url)),
            q => q.Exists(i => i.Field(f => f.LongDescription)),
            q => q.Exists(i => i.Field(f => f.Screenshots)),
            q => q.Exists(i => i.Field(f => f.Categories)),
            q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao)) 
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _telegramAppIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<List<TelegramAppIndex>> GetAllDisplayAsync(List<string> excludeAliases, int count, List<TelegramAppCategory> categories = null)
    {
        var mustNotQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>();
        if (!excludeAliases.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Terms(t => t.Field(f => f.Alias).Terms(excludeAliases)));
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Exists(i => i.Field(f => f.Url)),
            q => q.Exists(i => i.Field(f => f.LongDescription)),
            q => q.Exists(i => i.Field(f => f.Screenshots)),
            q => q.Exists(i => i.Field(f => f.Categories)),
            q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao)) 
        };
        if (categories != null && !categories.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(t => t.Field(f => f.Categories).Terms(categories)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.MustNot(mustNotQuery).Must(mustQuery));
        var searchRequest = new SearchDescriptor<TelegramAppIndex>()
            .Query(q => q
                .FunctionScore(fs => fs.Query(Filter)  
                    .Functions(fn => fn.RandomScore(rs => rs.Seed(DateTime.UtcNow.Ticks)))
                ));  
        var response = await _telegramAppIndexRepository.SearchAsync(searchRequest,0, count);
        return response.Documents.ToList();  
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

    public async Task<TelegramAppIndex> GetLatestCreatedAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Exists(i => i.Field(f => f.Url)),
            q => q.Exists(i => i.Field(f => f.LongDescription)),
            q => q.Exists(i => i.Field(f => f.Screenshots)),
            q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao)) 
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _telegramAppIndexRepository.GetAsync(Filter, sortType: SortOrder.Descending, sortExp: o => o.CreateTime);
    }

    public async Task<Tuple<long, List<TelegramAppIndex>>> GetByTimePeriodAsync(DateTime start, DateTime end, int skipCount, int maxResultCount)
    {
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(TimePeriodQuery(start, end)));
        var (count, list) = await _telegramAppIndexRepository.GetListAsync(Filter, sortType: SortOrder.Descending, 
            sortExp: o => o.CreateTime, skip: skipCount, limit: maxResultCount);
        return new Tuple<long, List<TelegramAppIndex>>(count, list);
    }

    public async Task<List<TelegramAppIndex>> GetAllByTimePeriodAsync(DateTime start, DateTime end)
    {
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(TimePeriodQuery(start, end)));
        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    private List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>> TimePeriodQuery(DateTime start, DateTime end)
    {
        return new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Exists(i => i.Field(f => f.Url)),
            q => q.Exists(i => i.Field(f => f.LongDescription)),
            q => q.Exists(i => i.Field(f => f.Screenshots)),
            q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao)),
            q => q.DateRange(r => r.Field(f => f.CreateTime).GreaterThan(start)),
            q => q.DateRange(r => r.Field(f => f.CreateTime).LessThanOrEquals(end))
        };
    }
}