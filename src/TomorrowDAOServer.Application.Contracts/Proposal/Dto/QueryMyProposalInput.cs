using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;

namespace TomorrowDAOServer.Proposal.Dto;

public class QueryMyProposalInput : IValidatableObject
{
    [Required] public string ChainId { get; set; }
    public string DAOId { get; set; }

    public string Alias { get; set; }
    public string ProposalId { get; set; }
    public string Address { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }
    }
}