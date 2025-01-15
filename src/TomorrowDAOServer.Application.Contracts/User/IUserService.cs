using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.User.Dtos;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.User;

public interface IUserService
{
    Task<UserSourceReportResultDto> UserSourceReportAsync(string chainId, string source);
    Task<bool> CompleteTaskAsync(CompleteTaskInput input);
    Task<VoteHistoryPagedResultDto<MyPointsDto>> GetMyPointsAsync(GetMyPointsInput input);
    Task<TaskListDto> GetTaskListAsync(string chainId);
    Task<long> ViewAdAsync(ViewAdInput input);
    Task<bool> SaveTgInfoAsync(SaveTgInfoInput input);
    Task<string> GetAdHashAsync(long timeStamp);
    Task<long> ClearAdCountAsync(string chainId, string address);
    Task GenerateDailyCreatePollPointsAsync(string chainId, List<IndexerProposal> proposalList);
    Task<LoginPointsStatusDto> GetLoginPointsStatusAsync(GetLoginPointsStatusInput input);
    Task<LoginPointsStatusDto> CollectLoginPointsAsync(CollectLoginPointsInput input);
    Task<HomePageResultDto> GetHomePageAsync(GetHomePageInput input);
    Task<PageResultDto<AppDetailDto>> GetMadeForYouAsync(GetMadeForYouInput input);
    Task<bool> OpenAppAsync(OpenAppInput input);
    Task<bool> CheckPointsAsync(string telegramAppId);
    Task<PagedResultDto<UserPointsDto>> GetAllUserPointsAsync(GetAllUserPointsInput input);
}