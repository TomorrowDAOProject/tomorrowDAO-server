using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Contract.Dto;

public class QueryContractsInfoInput : IValidatableObject
{
    [Required]
    public string ChainId { get; set; }
    
    public string DaoId { get; set; }
    public GovernanceMechanism? GovernanceMechanism { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }
    }
}