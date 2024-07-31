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
using TomorrowDAOServer.Treasury;
using TomorrowDAOServer.Treasury.Dto;
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
    private readonly ITreasuryAssetsService _treasuryAssetsService;

    public DiscussionService(IDiscussionProvider discussionProvider, ProposalProvider proposalProvider,
        IObjectMapper objectMapper, IUserService userService, IDAOProvider daoProvider, 
        ITreasuryAssetsService treasuryAssetsService)
    {
        _discussionProvider = discussionProvider;
        _proposalProvider = proposalProvider;
        _objectMapper = objectMapper;
        _userService = userService;
        _daoProvider = daoProvider;
        _treasuryAssetsService = treasuryAssetsService;
    }

    public async Task<NewCommentResultDto> NewCommentAsync(NewCommentInput input)
    {
        // todo only root comment now
        input.ParentId = CommonConstant.RootParentId;
        var userAddress = await _userService.GetCurrentUserAddressAsync(input.ChainId);
        if (string.IsNullOrEmpty(userAddress))
        {
            return new NewCommentResultDto { Reason = "Invalid user: not login." };
        }
        
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
        if (proposalIndex == null)
        {
            return new NewCommentResultDto { Reason = "Invalid proposalId: not existed." };
        }

        var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput { ChainId = proposalIndex.ChainId, DAOId = proposalIndex.DAOId });
        if (daoIndex == null)
        {
            return new NewCommentResultDto { Reason = "Invalid proposalId: dao not existed." };
        }

        if (string.IsNullOrEmpty(daoIndex.GovernanceToken))
        {
            var member = await _daoProvider.GetMemberAsync(new GetMemberInput { ChainId = input.ChainId, 
                DAOId = proposalIndex.DAOId, Address = userAddress });
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
        commentIndex.Id = GuidHelper.GenerateId(proposalIndex.ProposalId, count.ToString());
        commentIndex.Commenter = userAddress;
        commentIndex.CommentStatus = CommentStatusEnum.Normal;
        commentIndex.CreateTime = commentIndex.ModificationTime = TimeHelper.GetTimeStampInMilliseconds();
        await _discussionProvider.NewCommentAsync(commentIndex);
        
        return new NewCommentResultDto { Success = true};
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