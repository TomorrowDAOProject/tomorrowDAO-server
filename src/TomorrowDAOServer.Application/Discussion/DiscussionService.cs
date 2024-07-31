using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Discussion.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.User;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Discussion;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class DiscussionService : ApplicationService, IDiscussionService
{
    private readonly IDiscussionProvider _discussionProvider;
    private readonly ProposalProvider _proposalProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IUserService _userService;
    private readonly IDAOProvider _daoProvider;

    public DiscussionService(IDiscussionProvider discussionProvider, ProposalProvider proposalProvider,
        IObjectMapper objectMapper, IUserService userService, IDAOProvider daoProvider)
    {
        _discussionProvider = discussionProvider;
        _proposalProvider = proposalProvider;
        _objectMapper = objectMapper;
        _userService = userService;
        _daoProvider = daoProvider;
    }

    public async Task<bool> NewCommentAsync(NewCommentInput input)
    {
        // todo only root comment now
        input.ParentId = CommonConstant.RootParentId;
        var userAddress = await _userService.GetCurrentUserAddressAsync(input.ChainId);
        if (string.IsNullOrEmpty(userAddress))
        {
            throw new UserFriendlyException("Invalid user: not existed.");
        }
        
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        if (proposalIndex == null)
        {
            throw new UserFriendlyException("Invalid proposalId: not existed.");
        }

        var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput { ChainId = proposalIndex.ChainId, DAOId = proposalIndex.DAOId });
        if (daoIndex == null)
        {
            throw new UserFriendlyException("Invalid proposalId: dao not existed.");
        }

        if (string.IsNullOrEmpty(daoIndex.GovernanceToken))
        {
            var member = await _daoProvider.GetMemberAsync(new GetMemberInput { ChainId = input.ChainId, 
                DAOId = proposalIndex.DAOId, Address = userAddress });
            if (member.Address != userAddress)
            {
                throw new UserFriendlyException("Invalid user: not multi sig dao member.");
            }
        }
        else
        {
            // todo rely on treasury check
        }

        var count = await _discussionProvider.GetCommentCountAsync(input.ProposalId);
        if (count < 0)
        {
            return false;
        }

        var commentIndex = _objectMapper.Map<ProposalIndex, CommentIndex>(proposalIndex);
        _objectMapper.Map(input, commentIndex);
        commentIndex.Id = GuidHelper.GenerateId(proposalIndex.ProposalId, count.ToString());
        commentIndex.Commenter = userAddress;
        commentIndex.CommentStatus = CommentStatusEnum.Normal;
        commentIndex.CreateTime = commentIndex.ModificationTime = TimeHelper.GetTimeStampInMilliseconds();
        await _discussionProvider.NewCommentAsync(commentIndex);
        
        return true;
    }

    public async Task<PagedResultDto<CommentDto>> GetCommentListAsync(GetCommentListInput input)
    {
        var result = await _discussionProvider.GetRootCommentAsync(input);
        return new PagedResultDto<CommentDto>
        {
            TotalCount = result.Item1,
            Items = _objectMapper.Map<List<CommentIndex>, List<CommentDto>>(result.Item2)
        };
    }
}