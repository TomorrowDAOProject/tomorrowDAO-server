using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Google.Type;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class AddTeamDescInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string PublicKey { get; set; }
    [Required]
    public string Address { get; set; }
    [Required]
    public string Name { get; set; }
    public string Avatar { get; set; }
    public string Intro { get; set; }
    public string TxId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Socials { get; set; }
    public string OfficialWebsite { get; set; }
    public string Location { get; set; }
    public string Mail { get; set; }
    public DateTime UpdateTime { get; set; }
    
}

public class AddTeamDescResultDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; }
}