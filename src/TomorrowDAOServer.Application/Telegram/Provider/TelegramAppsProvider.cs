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
    Task BulkDeleteAsync(List<TelegramAppIndex> telegramAppIndices);
    Task<Tuple<long, List<TelegramAppIndex>>> GetTelegramAppsAsync(QueryTelegramAppsInput input, bool excludeDao = false);
    Task< List<TelegramAppIndex>> GetAllTelegramAppsAsync(QueryTelegramAppsInput input);
    Task<List<TelegramAppIndex>> GetNeedLoadDetailAsync();
    Task<List<TelegramAppIndex>> GetNeedSetCategoryAsync();
    Task<List<TelegramAppIndex>> GetNeedMigrateAppsAsync();
    Task<List<TelegramAppIndex>> GetAllDisplayAsync(List<string> excludeAliases, int count, List<TelegramAppCategory> categories = null);
    Task<Tuple<long, List<TelegramAppIndex>>> GetByCategoryAsync(TelegramAppCategory? category, int skipCount, int maxResultCount, List<string> aliases, string sort);
    Task<List<TelegramAppIndex>> SearchAppAsync(string title);
    Task<TelegramAppIndex> GetLatestCreatedAsync();
    Task<List<TelegramAppIndex>> GetAllByTimePeriodAsync(DateTime start, DateTime end);
    Task<long> CountByCategoryAsync(TelegramAppCategory? category);
    Task<List<TelegramAppIndex>> GetNeedUploadAsync(int skipCount);
    Task<Tuple<long, List<TelegramAppIndex>>> GetSearchListAsync(TelegramAppCategory? category, string search, int skipCount, int maxResultCount);
    Task<long> GetTotalPointsAsync();
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
        if (telegramAppIndex == null)
        {
            return;
        }
        telegramAppIndex.AppName = telegramAppIndex.Title;
        
        await _telegramAppIndexRepository.AddOrUpdateAsync(telegramAppIndex);
    }

    public async Task BulkAddOrUpdateAsync(List<TelegramAppIndex> telegramAppIndices)
    {
        if (telegramAppIndices == null || telegramAppIndices.IsNullOrEmpty())
        {
            return;
        }

        foreach (var telegramAppIndex in telegramAppIndices)
        {
            telegramAppIndex.AppName = telegramAppIndex.Title;
        }
        
        await _telegramAppIndexRepository.BulkAddOrUpdateAsync(telegramAppIndices);
    }
    
    public async Task BulkDeleteAsync(List<TelegramAppIndex> telegramAppIndices)
    {
        if (telegramAppIndices == null || telegramAppIndices.IsNullOrEmpty())
        {
            return;
        }
        await _telegramAppIndexRepository.BulkDeleteAsync(telegramAppIndices);
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

        if (!input.SourceTypes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.SourceType).Terms(input.SourceTypes)));
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

        if (!input.SourceTypes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.SourceType).Terms(input.SourceTypes)));
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

    public async Task<List<TelegramAppIndex>> GetNeedMigrateAppsAsync()
    {
        var sourceTypes = new List<SourceType>() { SourceType.Telegram, SourceType.FindMini };
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>(); 
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.SourceType).Terms(sourceTypes)));
        
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));

        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    public async Task<List<TelegramAppIndex>> GetAllDisplayAsync(List<string> excludeAliases, int count, List<TelegramAppCategory> categories = null)
    {
        var mustNotQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>();
        if (!excludeAliases.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Terms(t => t.Field(f => f.Alias).Terms(excludeAliases)));
        }

        var mustQuery = DisplayQuery();
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

    public async Task<Tuple<long, List<TelegramAppIndex>>> GetByCategoryAsync(TelegramAppCategory? category, int skipCount, int maxResultCount, List<string> aliases, string sort)
    {
        var mustQuery = DisplayQuery();
        if (category != null && category != TelegramAppCategory.All)
        {
            mustQuery.Add(q => q.Terms(t => t.Field(f => f.Categories).Terms(category)));
        }
        if (!aliases.IsNullOrEmpty())
        {
            mustQuery.Add(q => !q.Terms(t => t.Field(f => f.Alias).Terms(aliases)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> SortDescriptor(SortDescriptor<TelegramAppIndex> s) { return sort == "TotalPoints" ?
            s.Descending(p => p.TotalPoints) : s.Descending(p => p.CreateTime); }
        return await _telegramAppIndexRepository.GetSortListAsync(Filter, skip: skipCount, limit: maxResultCount, sortFunc: SortDescriptor);
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
        var mustQuery = DisplayQuery();
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _telegramAppIndexRepository.GetAsync(Filter, sortType: SortOrder.Descending, sortExp: o => o.CreateTime);
    }

    public async Task<List<TelegramAppIndex>> GetAllByTimePeriodAsync(DateTime start, DateTime end)
    {
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(TimePeriodQuery(start, end)));
        return await IndexHelper.GetAllIndex(Filter, _telegramAppIndexRepository);
    }

    public async Task<long> CountByCategoryAsync(TelegramAppCategory? category)
    {
        var mustQuery = DisplayQuery();
        if (category != null)
        {
            mustQuery.Add(q => q.Terms(t => t.Field(f => f.Categories).Terms(category)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _telegramAppIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<List<TelegramAppIndex>> GetNeedUploadAsync(int skipCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Bool(b => b
                .Should(
                    s => s.Bool(bs => bs
                        .Must(
                            m => m.Exists(e => e.Field(f => f.Icon)),
                            m => !m.Exists(e => e.Field(f => f.BackIcon))
                        )),
                    s => s.Bool(bs => bs
                        .Must(
                            m => m.Exists(e => e.Field(f => f.Screenshots)), 
                            m => !m.Exists(e => e.Field(f => f.BackScreenshots))
                        ))
                )
                .MinimumShouldMatch(1)
            ),
            q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao))
        };

        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
    
        return (await _telegramAppIndexRepository.GetListAsync(Filter, skip: skipCount, limit: 10)).Item2;
    }

    public async Task<Tuple<long, List<TelegramAppIndex>>> GetSearchListAsync(TelegramAppCategory? category, string search, int skipCount, int maxResultCount)
    {
        var mustQuery = DisplayQuery();
        mustQuery.Add(q =>
            q.Wildcard(i => i.Field(t => t.AppName).Value($"*{search}*")));
        if (category != null && category != TelegramAppCategory.All)
        {
            mustQuery.Add(q => q.Terms(t => t.Field(f => f.Categories).Terms(category)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TelegramAppIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _telegramAppIndexRepository.GetListAsync(Filter, skip: skipCount, limit: maxResultCount);
    }

    public async Task<long> GetTotalPointsAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Exists(i => i.Field(f => f.TotalPoints))
        };
        var query = new SearchDescriptor<TelegramAppIndex>().Size(0)
            .Query(q => q.Exists(i => i.Field(f => f.TotalPoints)))
            .Aggregations(a => a.Sum("total_points_sum", sum => sum.Field(f => f.TotalPoints)));
        var searchResponse = await _telegramAppIndexRepository.SearchAsync(query, 0, Int32.MaxValue);
        var totalPointsSum = searchResponse.Aggregations.Sum("total_points_sum").Value;
        return totalPointsSum.HasValue ? (long)totalPointsSum.Value : 0L;
    }

    private List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>> TimePeriodQuery(DateTime start, DateTime end)
    {
        var query = DisplayQuery();
        query.Add(q => q.DateRange(r => r.Field(f => f.CreateTime).GreaterThan(start)));
        query.Add(q => q.DateRange(r => r.Field(f => f.CreateTime).LessThanOrEquals(end)));
        return query;
    }

    private List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>> DisplayQuery()
    {
        return new List<Func<QueryContainerDescriptor<TelegramAppIndex>, QueryContainer>>
        {
            q => q.Exists(i => i.Field(f => f.Url)),
            q => q.Exists(i => i.Field(f => f.LongDescription)),
            q => q.Exists(i => i.Field(f => f.BackScreenshots)),
            q => q.Exists(i => i.Field(f => f.BackIcon)),
            q => q.Exists(i => i.Field(f => f.Categories)),
            q => !q.Term(i => i.Field(f => f.SourceType).Value(SourceType.TomorrowDao)),
        };
    }
}