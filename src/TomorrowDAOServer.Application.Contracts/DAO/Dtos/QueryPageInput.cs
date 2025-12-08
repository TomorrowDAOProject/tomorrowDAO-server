using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.DAO.Dtos;

public class QueryPageInput : IValidatableObject
{
    [Required] public string ChainId { get; set; }

    [Range(0, int.MaxValue)] public int SkipCount { get; set; } = 0;
    
    [Range(1, int.MaxValue)]
    public int MaxResultCount { get; set; } = 6;
    
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }
    }
}