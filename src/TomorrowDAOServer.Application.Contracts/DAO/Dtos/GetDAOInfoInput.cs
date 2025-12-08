using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.DAO.Dtos;

public class GetDAOInfoInput
{
    [Required] public string ChainId { get; set; }
    public string DAOId { get; set; }

    public string Alias { get; set; }
}