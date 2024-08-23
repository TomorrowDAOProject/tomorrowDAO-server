using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Proposal.Dto;

public class QueryVoteHistoryInput : IValidatableObject
{
    [Required] public string ChainId { get; set; }
    // [Required] 
    public string DAOId { get; set; }
    public string ProposalId { get; set; } = string.Empty;
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 100;
    public string VoteOption { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Source { get; set; } = VoteHistorySource.TomorrowDao.ToString();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult("ChainId invalid.");
        }

        if (!VoteOption.IsNullOrEmpty() && !Enum.TryParse<VoteOption>(VoteOption, out _))
        {
            yield return new ValidationResult("VoteOption invalid.");
        }
    }
}