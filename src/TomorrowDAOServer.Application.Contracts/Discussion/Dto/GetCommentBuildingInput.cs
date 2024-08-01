using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discussion.Dto;

public class GetCommentBuildingInput
{
    [Required] public string ChainId { get; set; }
    [Required] public string ProposalId { get; set; }
}