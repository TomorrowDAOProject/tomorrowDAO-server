using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class GetTeamDescInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string PublicKey { get; set; }
}

public class GetTeamDescResultDto
{
    public string ChainId { get; set; }
    public string PublicKey { get; set; }
    public string Address { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public string Intro { get; set; }
    public string TxId { get; set; }
    public bool IsActive { get; set; }
    public List<string> Socials { get; set; }
    public string OfficialWebsite { get; set; }
    public string Location { get; set; }
    public string Mail { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}