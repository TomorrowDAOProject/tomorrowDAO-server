using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.DAO.Dtos;

public class GetMemberListInput : GetDAOInfoInput
{
    [Range(0, int.MaxValue)] 
    public int SkipCount { get; set; } = 0;
    [Range(1, int.MaxValue)]
    public int MaxResultCount { get; set; } = 20;
}