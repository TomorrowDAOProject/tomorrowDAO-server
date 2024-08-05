using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
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

    public async Task<long> GetCommentCountAsync(string proposalId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ICommentCountGrain>(proposalId);
            return await grain.GetNextCount();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetCommentCountAsyncException proposalId {proposalId}", proposalId);
            return -1;
        }
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
}