using System;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;
using ProposalType = TomorrowDAOServer.Common.Enum.ProposalType;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class GetAppliedListInput : PagedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string Address { get; set; }
    public NetworkDaoOrgType ProposalType { get; set; } = NetworkDaoOrgType.Parliament;
    public string Search { get; set; }
}

public class GetAppliedListPagedResult : PagedResultDto<GetAppliedListResultDto>
{
    
}

public class GetAppliedListResultDto
{
    public DateTime CreateAt { get; set; }
    public DateTime ExpiredTime { get; set; }
    public string ProposalId { get; set; }
    public string CreateTxId { get; set; }
    public NetworkDaoProposalStatusEnum Status { get; set; }
}