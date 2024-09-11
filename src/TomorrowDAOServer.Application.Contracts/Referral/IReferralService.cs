using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Referral.Dto;

namespace TomorrowDAOServer.Referral;

public interface IReferralService
{
    // Task<GetLinkDto> GetLinkAsync(string token, string chainId);
    Task<InviteDetailDto> InviteDetailAsync(string chainId);
    Task<PageResultDto<InviteLeaderBoardDto>> InviteLeaderBoardAsync(InviteLeaderBoardInput input);
    List<Tuple<long, long>> ConfigAsync();
}