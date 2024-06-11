using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.DAO.Dtos;

public class QueryDAOListInput : IValidatableObject
{
    public QueryDAOListInput()
    {
        SkipCount = 0;
    }

    [Required] public string ChainId { get; set; }

    [Range(0, int.MaxValue)] 
    public int SkipCount { get; set; }
    
    [Range(1, int.MaxValue)]
    public int MaxResultCount { get; set; } = 6;
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }
    }
}