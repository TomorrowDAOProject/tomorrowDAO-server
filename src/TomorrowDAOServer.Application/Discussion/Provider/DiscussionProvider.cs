using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Grains.Grain.Discussion;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Discussion.Provider;

public interface IDiscussionProvider
{
    Task<long> GetCommentCountAsync(string proposalId);
    Task NewCommentAsync(CommentIndex index);
    Task<long> CountCommentListAsync(GetCommentListInput input);
    Task<Tuple<long, List<CommentIndex>>> GetCommentListAsync(GetCommentListInput input);
    Task<CommentIndex> GetCommentAsync(string id);
    Task<Tuple<long, List<CommentIndex>>> GetAllCommentsByProposalIdAsync(string chainId, string proposalId);
    Task<Tuple<long, List<CommentIndex>>> GetEarlierAsync(string id, string proposalId, long time, int maxResultCount);
    Task<Dictionary<string, long>> GetAppCommentCountAsync(List<string> aliases);
}

public class DiscussionProvider : IDiscussionProvider, ISingletonDependency
{
    private readonly INESTRepository<CommentIndex, string> _commentIndexRepository;
    private readonly ILogger<DiscussionProvider> _logger;
    private readonly IClusterClient _clusterClient;

    public DiscussionProvider(ILogger<DiscussionProvider> logger, 
        INESTRepository<CommentIndex, string> commentIndexRepository, 
        IClusterClient clusterClient)
    {
        _commentIndexRepository = commentIndexRepository;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),  
        MethodName = nameof(TmrwDaoExceptionHandler.HandleGetCommentCountAsync))]
    public virtual async Task<long> GetCommentCountAsync(string proposalId)
    {
        var grain = _clusterClient.GetGrain<ICommentCountGrain>(proposalId);
        return await grain.GetNextCount();
    }

    public async Task NewCommentAsync(CommentIndex index)
    {
        await _commentIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<long> CountCommentListAsync(GetCommentListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CommentIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)),
            q => q.Term(i => i.Field(t => t.ProposalId).Value(input.ProposalId)),
            q => q.Term(i => i.Field(t => t.ParentId).Value(input.ParentId))
        };
        QueryContainer Filter(QueryContainerDescriptor<CommentIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _commentIndexRepository.CountAsync(Filter)).Count;
    }

    public async Task<Tuple<long, List<CommentIndex>>> GetCommentListAsync(GetCommentListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CommentIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)),
            q => q.Term(i => i.Field(t => t.ProposalId).Value(input.ProposalId)),
            q => q.Term(i => i.Field(t => t.ParentId).Value(input.ParentId))
        };
        QueryContainer Filter(QueryContainerDescriptor<CommentIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _commentIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount,
            sortFunc: _ => new SortDescriptor<CommentIndex>().Descending(index => index.CreateTime));
    }

    public async Task<CommentIndex> GetCommentAsync(string id)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CommentIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.Id).Value(id)),
            q => !q.Term(i => i.Field(t => t.CommentStatus).Value(CommentStatusEnum.Deleted))
        };
        QueryContainer Filter(QueryContainerDescriptor<CommentIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _commentIndexRepository.GetAsync(Filter);
    }
    
    public async Task<Tuple<long, List<CommentIndex>>> GetAllCommentsByProposalIdAsync(string chainId, string proposalId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CommentIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(chainId)),
            q => q.Term(i => i.Field(t => t.ProposalId).Value(proposalId)),
        };
        QueryContainer Filter(QueryContainerDescriptor<CommentIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _commentIndexRepository.GetListAsync(Filter);
    }
    
    public async Task<Tuple<long, List<CommentIndex>>> GetEarlierAsync(string id, string proposalId, long time, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CommentIndex>, QueryContainer>>
        {
            q => !q.Term(i => i.Field(t => t.Id).Value(id)),
            q => q.Term(i => i.Field(t => t.ProposalId).Value(proposalId)),
            q => q.TermRange(i => i.Field(t => t.CreateTime).LessThanOrEquals(time.ToString()))
        };
        QueryContainer Filter(QueryContainerDescriptor<CommentIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _commentIndexRepository.GetSortListAsync(Filter, skip: 0, limit: maxResultCount,
            sortFunc: _ => new SortDescriptor<CommentIndex>().Descending(index => index.CreateTime));
    }

    public async Task<Dictionary<string, long>> GetAppCommentCountAsync(List<string> aliases)
    {
        if (aliases == null || aliases.IsNullOrEmpty())
        {
            return new Dictionary<string, long>();
        }
        var query = new SearchDescriptor<CommentIndex>().Size(0)
            .Query(q => q.Terms(t => t.Field(f => f.ProposalId).Terms(aliases)))
            .Aggregations(a => a.Terms("by_alias", ta => ta.Field(f => f.ProposalId).Size(aliases.Count)));
        var response = await _commentIndexRepository.SearchAsync(query, 0, int.MaxValue);
        var aliasCountMap = response.Aggregations?.Terms("by_alias")?.Buckets
            .ToDictionary(b => b.Key, b => b.DocCount.GetValueOrDefault());
        return aliasCountMap ?? new Dictionary<string, long>();
    }
}