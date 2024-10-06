using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Discussion.Dto;

public class CommentBaseInput : IValidatableObject
{
    [Required] public string ChainId { get; set; }
    public string ProposalId { get; set; }
    public string Alias { get; set; }
    public string ParentId { get; set; } = CommonConstant.RootParentId;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(ProposalId) && string.IsNullOrEmpty(Alias))
        {
            yield return new ValidationResult("Both proposalId and alias is empty.");
        }
        
        if (!string.IsNullOrEmpty(ProposalId) && !string.IsNullOrEmpty(Alias))
        {
            yield return new ValidationResult("Both proposalId and alias is not empty.");
        }
    }
}