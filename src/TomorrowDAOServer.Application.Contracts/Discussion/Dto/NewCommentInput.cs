using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Discussion.Dto;

public class NewCommentInput : IValidatableObject
{
    [Required] public string ChainId { get; set; }
    [Required] public string ProposalId { get; set; }
    [Required] public string Comment { get; set; }
    public string ParentId { get; set; } = CommonConstant.RootParentId;
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(ProposalId) || string.IsNullOrEmpty(Comment) || string.IsNullOrEmpty(ChainId))
        {
            yield return new ValidationResult($"Invalid input.");
        }
    }
}