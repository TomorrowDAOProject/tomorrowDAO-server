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
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.Treasury;
using TomorrowDAOServer.Treasury.Dto;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
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
    private readonly ITelegramAppsProvider _telegramAppsProvider;

    public DiscussionService(IDiscussionProvider discussionProvider, ProposalProvider proposalProvider,
        IObjectMapper objectMapper, IUserProvider userProvider, IDAOProvider daoProvider,
        ITreasuryAssetsService treasuryAssetsService, ITelegramAppsProvider telegramAppsProvider)
    {
        _discussionProvider = discussionProvider;
        _proposalProvider = proposalProvider;
        _objectMapper = objectMapper;
        _userProvider = userProvider;
        _daoProvider = daoProvider;
        _treasuryAssetsService = treasuryAssetsService;
        _telegramAppsProvider = telegramAppsProvider;
    }

    public async Task<NewCommentResultDto> NewCommentAsync(NewCommentInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        
        if (input.ParentId != CommonConstant.RootParentId)
        {
            var parentComment = await _discussionProvider.GetCommentAsync(input.ParentId);
            if (parentComment == null || string.IsNullOrEmpty(parentComment.Commenter))
            {
                return new NewCommentResultDto { Reason = "Invalid parentId: not existed." };
            }

            if (parentComment.Commenter == address || parentComment.CommenterId == userId)
            {
                return new NewCommentResultDto { Reason = "Invalid parentId: can not comment self." };
            }
        }

        return string.IsNullOrEmpty(input.ProposalId) ? await CommentApp(address, userGrainDto, input) : await CommentProposal(address, userGrainDto, input);
    }

    private async Task<NewCommentResultDto> CommentApp(string address, UserGrainDto userGrainDto, NewCommentInput input)
    {
        var (count, app) = await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = new List<string> { input.Alias }
        });
        if (count == 0)
        {
            return new NewCommentResultDto { Reason = "Invalid alias: not existed." };
        }
        var commentIndex = _objectMapper.Map<NewCommentInput, CommentIndex>(input);
        commentIndex.ProposalId = input.Alias;
        return await Comment(address, userGrainDto, input.Alias, commentIndex);
    }
    
    private async Task<NewCommentResultDto> CommentProposal(string address, UserGrainDto userGrainDto,
        NewCommentInput input)
    {
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
                DAOId = proposalIndex.DAOId, Address = address
            });
            if (member.Address != address)
            {
                return new NewCommentResultDto { Reason = "Invalid proposalId: not multi sig dao member." };
            }
        }
        else
        {
            var isDepositor = await _treasuryAssetsService.IsTreasuryDepositorAsync(new IsTreasuryDepositorInput
            {
                ChainId = input.ChainId, Address = address,
                GovernanceToken = daoIndex.GovernanceToken, TreasuryAddress = daoIndex.TreasuryAccountAddress
            });
            if (!isDepositor)
            {
                return new NewCommentResultDto { Reason = "Invalid proposalId: not depositor." };
            }
        }

        var commentIndex = _objectMapper.Map<ProposalIndex, CommentIndex>(proposalIndex);
        _objectMapper.Map(input, commentIndex);
        return await Comment(address, userGrainDto, input.ProposalId, commentIndex);
    }

    private async Task<NewCommentResultDto> Comment(string address, UserGrainDto userGrainDto, string commentSubject,
        CommentIndex commentIndex)
    {
        var count = await _discussionProvider.GetCommentCountAsync(commentSubject);
        if (count < 0)
        {
            return new NewCommentResultDto { Reason = "Retry later." };
        }

        var now = TimeHelper.GetTimeStampInMilliseconds();
        commentIndex.Id = GuidHelper.GenerateId(commentSubject, now.ToString(), count.ToString());
        commentIndex.Commenter = address;
        commentIndex.CommenterId = userGrainDto.UserId.ToString();
        commentIndex.CommentStatus = CommentStatusEnum.Normal;
        var telegramAuthDataDto = userGrainDto.GetUserInfo();
        if (telegramAuthDataDto != null)
        {
            commentIndex.CommenterName = telegramAuthDataDto.UserName;
            commentIndex.CommenterFirstName = telegramAuthDataDto.FirstName;
            commentIndex.CommenterLastName = telegramAuthDataDto.LastName;
            commentIndex.CommenterPhoto = telegramAuthDataDto.PhotoUrl;
        }
        commentIndex.CreateTime = commentIndex.ModificationTime = now;
        await _discussionProvider.NewCommentAsync(commentIndex);

        return new NewCommentResultDto { Success = true, Comment = commentIndex };
    }

    public async Task<CommentListPageResultDto> GetCommentListAsync(GetCommentListInput input)
    {
        var isCommentProposal = string.IsNullOrEmpty(input.Alias);
        input.ProposalId = isCommentProposal ? input.ProposalId : input.Alias;
        CommentListPageResultDto list;
        if (string.IsNullOrEmpty(input.SkipId))
        {
            var result = await _discussionProvider.GetCommentListAsync(input);
            list = new CommentListPageResultDto
            {
                TotalCount = result.Item1,
                Items = _objectMapper.Map<List<CommentIndex>, List<CommentDto>>(result.Item2),
                HasMore = result.Item1 > input.SkipCount + input.MaxResultCount
            };
        }
        else
        {
            var comment = await _discussionProvider.GetCommentAsync(input.SkipId) ?? new CommentIndex();
            var totalCount = await _discussionProvider.CountCommentListAsync(input);
            var result1 = await _discussionProvider.GetEarlierAsync(input.SkipId, input.ProposalId, comment.CreateTime,
                input.MaxResultCount);
            list = new CommentListPageResultDto
            {
                TotalCount = totalCount,
                Items = _objectMapper.Map<List<CommentIndex>, List<CommentDto>>(result1.Item2),
                HasMore = result1.Item1 > input.SkipCount + input.MaxResultCount
            };
        }

        if (isCommentProposal)
        {
            return list;
        }

        foreach (var commentDto in list.Items)
        {
            commentDto.Alias = commentDto.ProposalId;
            commentDto.ProposalId = string.Empty;
        }

        return list;
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