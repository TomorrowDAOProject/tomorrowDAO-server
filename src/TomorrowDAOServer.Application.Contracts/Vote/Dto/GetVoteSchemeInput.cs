using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Vote.Dto;

public class GetVoteSchemeInput : IValidatableObject
{
    [Required] public string ChainId { get; set; }
    public string DAOId { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }
    }
}