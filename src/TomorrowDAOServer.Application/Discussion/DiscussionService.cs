using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Discussion.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Treasury;
using TomorrowDAOServer.Treasury.Dto;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Discussion;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class DiscussionService : ApplicationService, IDiscussionService
{
    private readonly IDiscussionProvider _discussionProvider;
    private readonly ProposalProvider _proposalProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IUserProvider _userProvider;
    private readonly IDAOProvider _daoProvider;
    private readonly ITreasuryAssetsService _treasuryAssetsService;

    public DiscussionService(IDiscussionProvider discussionProvider, ProposalProvider proposalProvider,
        IObjectMapper objectMapper, IUserProvider userProvider, IDAOProvider daoProvider,
        ITreasuryAssetsService treasuryAssetsService)
    {
        _discussionProvider = discussionProvider;
        _proposalProvider = proposalProvider;
        _objectMapper = objectMapper;
        _userProvider = userProvider;
        _daoProvider = daoProvider;
        _treasuryAssetsService = treasuryAssetsService;
    }

    public async Task<NewCommentResultDto> NewCommentAsync(NewCommentInput input)
    {
        var userAddress =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (input.ParentId != CommonConstant.RootParentId)
        {
            var parentComment = await _discussionProvider.GetCommentAsync(input.ParentId);
            if (parentComment == null || string.IsNullOrEmpty(parentComment.Commenter))
            {
                return new NewCommentResultDto { Reason = "Invalid parentId: not existed." };
            }

            if (parentComment.Commenter == userAddress)
            {
                return new NewCommentResultDto { Reason = "Invalid parentId: can not comment self." };
            }
        }

        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        if (proposalIndex == null)
        {
            return new NewCommentResultDto { Reason = "Invalid proposalId: not existed." };
        }

        var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
            { ChainId = proposalIndex.ChainId, DAOId = proposalIndex.DAOId });
        if (daoIndex == null)
        {
            return new NewCommentResultDto { Reason = "Invalid proposalId: dao not existed." };
        }

        if (string.IsNullOrEmpty(daoIndex.GovernanceToken))
        {
            var member = await _daoProvider.GetMemberAsync(new GetMemberInput
            {
                ChainId = input.ChainId,
                DAOId = proposalIndex.DAOId, Address = userAddress
            });
            if (member.Address != userAddress)
            {
                return new NewCommentResultDto { Reason = "Invalid proposalId: not multi sig dao member." };
            }
        }
        else
        {
            var isDepositor = await _treasuryAssetsService.IsTreasuryDepositorAsync(new IsTreasuryDepositorInput
            {
                ChainId = input.ChainId, Address = userAddress,
                GovernanceToken = daoIndex.GovernanceToken, TreasuryAddress = daoIndex.TreasuryAccountAddress
            });
            if (!isDepositor)
            {
                return new NewCommentResultDto { Reason = "Invalid proposalId: not depositor." };
            }
        }

        var count = await _discussionProvider.GetCommentCountAsync(input.ProposalId);
        if (count < 0)
        {
            return new NewCommentResultDto { Reason = "Retry later." };
        }

        var commentIndex = _objectMapper.Map<ProposalIndex, CommentIndex>(proposalIndex);
        _objectMapper.Map(input, commentIndex);
        var now = TimeHelper.GetTimeStampInMilliseconds();
        commentIndex.Id = GuidHelper.GenerateId(proposalIndex.ProposalId, now.ToString(), count.ToString());
        commentIndex.Commenter = userAddress;
        commentIndex.CommentStatus = CommentStatusEnum.Normal;
        commentIndex.CreateTime = commentIndex.ModificationTime = now;
        await _discussionProvider.NewCommentAsync(commentIndex);

        return new NewCommentResultDto { Success = true, Comment = commentIndex };
    }

    public async Task<CommentListPageResultDto> GetCommentListAsync(GetCommentListInput input)
    {
        if (string.IsNullOrEmpty(input.SkipId))
        {
            var result = await _discussionProvider.GetCommentListAsync(input);
            return new CommentListPageResultDto
            {
                TotalCount = result.Item1,
                Items = _objectMapper.Map<List<CommentIndex>, List<CommentDto>>(result.Item2),
                HasMore = result.Item1 > input.SkipCount + input.MaxResultCount
            };
        }

        var comment = await _discussionProvider.GetCommentAsync(input.SkipId) ?? new CommentIndex();
        var totalCount = await _discussionProvider.CountCommentListAsync(input);
        var result1 = await _discussionProvider.GetEarlierAsync(input.SkipId, input.ProposalId, comment.CreateTime,
            input.MaxResultCount);
        return new CommentListPageResultDto
        {
            TotalCount = totalCount,
            Items = _objectMapper.Map<List<CommentIndex>, List<CommentDto>>(result1.Item2),
            HasMore = result1.Item1 > input.SkipCount + input.MaxResultCount
        };
    }

    public async Task<CommentBuildingDto> GetCommentBuildingAsync(GetCommentBuildingInput input)
    {
        var allComments = await _discussionProvider.GetAllCommentsByProposalIdAsync(input.ChainId, input.ProposalId);
        var commentMap = allComments.Item2.GroupBy(x => x.ParentId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var building = new CommentBuilding { Id = CommonConstant.RootParentId, Comment = null };
        GenerateCommentBuilding(building, commentMap);
        return new CommentBuildingDto
        {
            CommentBuilding = building, TotalCount = allComments.Item1
        };
    }

    private void GenerateCommentBuilding(CommentBuilding building,
        IReadOnlyDictionary<string, List<CommentIndex>> commentMap)
    {
        if (!commentMap.TryGetValue(building.Id, out var subCommentList))
        {
            return;
        }

        subCommentList = subCommentList.OrderByDescending(x => x.CreateTime).ToList();
        foreach (var subBuilding in subCommentList.Select(subComment => new CommentBuilding
                 {
                     Id = subComment.Id, Comment = ObjectMapper.Map<CommentIndex, CommentDto>(subComment)
                 }))
        {
            GenerateCommentBuilding(subBuilding, commentMap);
            building.SubComments.Add(subBuilding);
        }
    }
}