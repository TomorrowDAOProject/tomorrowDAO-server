using System;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class AddContractNameInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string ContractName { get; set; }
    [Required]
    public string TxId { get; set; }
    public NetworkDaoContractNameActionEnum Action { get; set; }
    [Required]
    public string Address { get; set; }
    
    public string ProposalId { get; set; }
    public DateTime CreateAt { get; set; }
}

public class AddContractNameResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}