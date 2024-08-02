using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Discussion.Dto;

public class GetCommentListInput
{
    [Required] public string ChainId { get; set; }
    [Required] public string ProposalId { get; set; }
    public string ParentId { get; set; } = CommonConstant.RootParentId;
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 6;
    public string SkipId { get; set; } = string.Empty;
}