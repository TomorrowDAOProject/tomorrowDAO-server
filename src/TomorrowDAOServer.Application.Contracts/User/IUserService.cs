using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.User.Dtos;

namespace TomorrowDAOServer.User;

public interface IUserService
{
    Task<UserSourceReportResultDto> UserSourceReportAsync(string chainId, string source);
    Task<bool> CompleteTaskAsync(CompleteTaskInput input);
    Task<VoteHistoryPagedResultDto<MyPointsDto>> GetMyPointsAsync(GetMyPointsInput input);
    Task<TaskListDto> GetTaskListAsync(string chainId);
    Task<long> ViewAdAsync(ViewAdInput input);
    Task<bool> SaveTgInfoAsync(SaveTgInfoInput input);
    Task GenerateDailyCreatePollPointsAsync(string chainId, List<IndexerProposal> proposalList);
}