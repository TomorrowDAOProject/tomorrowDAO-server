using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discussion.Dto;

public class GetCommentListInput
{
    [Required] public string ChainId { get; set; }
    [Required] public string ProposalId { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 6;
}