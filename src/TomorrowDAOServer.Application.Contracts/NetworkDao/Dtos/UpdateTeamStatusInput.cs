using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.NetworkDao.Dtos;

public class UpdateTeamStatusInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public bool IsActive { get; set; } = true;
    [Required]
    public string PublicKey { get; set; }
    [Required]
    public string Name { get; set; }
}

public class UpdateTeamStatusResultDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; }
}