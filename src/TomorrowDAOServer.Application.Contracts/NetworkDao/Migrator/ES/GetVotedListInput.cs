using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetVotedListInput : PagedAndSortedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    public string ProposalId { get; set; }
    public List<string> ProposalIds { get; set; }
    public string Search { get; set; }
    public NetworkDaoOrgType ProposalType { get; set; }
    public string Address { get; set; }
    
}

public class GetVotedListPagedResult : PagedResultDto<GetVotedListResultDto>
{
}

public class GetVotedListResultDto
{
    public long Amount { get; set; }
    public DateTime Time { get; set; }
    public string TxId { get; set; }
    public string Voter { get; set; }
    public string Symbol { get; set; } = "none";
    public NetworkDaoReceiptTypeEnum Action { get; set; }
}

