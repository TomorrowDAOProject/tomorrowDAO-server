using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Referral.Dto;

namespace TomorrowDAOServer.Referral;

public interface IReferralService
{
    // Task<GetLinkDto> GetLinkAsync(string token, string chainId);
    Task<InviteDetailDto> InviteDetailAsync(string chainId);
    Task<InviteBoardPageResultDto<InviteLeaderBoardDto>> InviteLeaderBoardAsync(InviteLeaderBoardInput input);
    ReferralActiveConfigDto ConfigAsync();
    Task<ReferralBindingStatusDto> ReferralBindingStatusAsync(string chainId);
}