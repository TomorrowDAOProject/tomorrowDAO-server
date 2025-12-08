using System;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class GetAllPersonalVotesInput : PagedAndSortedResultRequestDto
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string Address { get; set; }
    [Required]
    public NetworkDaoOrgType ProposalType { get; set; }
    //ProposalId
    public string Search { get; set; }
}

public class GetAllPersonalVotesPagedResult : PagedResultDto<GetPersonalVotesResultDto>
{
    
}

public class GetPersonalVotesResultDto
{
    public string Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Time { get; set; }
    public string ProposalId { get; set; }
    public string TxId { get; set; }
    public string Voter { get; set; }
    public string Symbol { get; set; }
    public NetworkDaoReceiptTypeEnum Action { get; set; }
    public bool Claimed { get; set; } = false;
    public string ClaimedTx { get; set; }
    public DateTime ClaimedTime { get; set; }
}