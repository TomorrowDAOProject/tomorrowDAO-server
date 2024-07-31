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
    Task<Tuple<long, List<CommentIndex>>> GetRootCommentAsync(GetCommentListInput input);
    Task<bool> GetCommentExistedAsync(string parentId);
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

    public async Task<Tuple<long, List<CommentIndex>>> GetRootCommentAsync(GetCommentListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CommentIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.ChainId).Value(input.ChainId)),
            q => q.Term(i => i.Field(t => t.ProposalId).Value(input.ProposalId)),
            q => q.Term(i => i.Field(t => t.ParentId).Value(CommonConstant.RootParentId))
        };
        QueryContainer Filter(QueryContainerDescriptor<CommentIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _commentIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount,
            sortFunc: _ => new SortDescriptor<CommentIndex>().Descending(index => index.CreateTime));
    }

    public async Task<bool> GetCommentExistedAsync(string parentId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CommentIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(t => t.Id).Value(parentId)),
            q => !q.Term(i => i.Field(t => t.CommentStatus).Value(CommentStatusEnum.Deleted))
        };
        QueryContainer Filter(QueryContainerDescriptor<CommentIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _commentIndexRepository.CountAsync(Filter)).Count > 0;
    }
}