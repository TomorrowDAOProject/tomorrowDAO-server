using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Proposal.Dto;

public class QueryExecutableProposalsInput : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }

    [Required] public string DaoId { get; set; }

    [Required] public string Proposer { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }

        if (DaoId.IsNullOrWhiteSpace())
        {
            yield return new ValidationResult($"DaoId invalid.");
        }

        if (Proposer.IsNullOrWhiteSpace())
        {
            yield return new ValidationResult($"Proposer invalid.");
        }
    }
}