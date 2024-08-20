using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Proposal.Dto;

public class VoteHistoryPagedResultDto<T> : PagedResultDto<T>
{
    public VoteHistoryPagedResultDto(long totalCount, IReadOnlyList<T> data, long totalPoints) : base(totalCount, data)
    {
        TotalPoints = totalPoints;
    }

    public long TotalPoints { get; set; }
}