using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.DAO.Dtos;

public class IsDaoMemberInput
{
    [Required] 
    public string ChainId { get; set; }
    [Required] 
    public string DAOId { get; set; }
    [Required]
    public string MemberAddress { get; set; }
}