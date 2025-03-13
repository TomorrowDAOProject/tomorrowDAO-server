using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class GetAllTeamDescInput
{
    [Required]
    public string ChainId { get; set; }
    public bool IsActive { get; set; } = true;
}