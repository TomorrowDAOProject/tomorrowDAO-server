using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;
using ProposalType = TomorrowDAOServer.Enums.ProposalType;

namespace TomorrowDAOServer.Proposal.Dto;

public class QueryProposalListInput : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }

    [Required] public string DaoId { get; set; }

    public GovernanceMechanism? GovernanceMechanism { get; set; }

    public ProposalType? ProposalType { get; set; }

    public ProposalStatus? ProposalStatus { get; set; }

    public string Content { get; set; }

    public bool IsNetworkDao { get; set; }
    
    public PageInfo PageInfo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
        {
            yield return new ValidationResult($"ChainId invalid.");
        }
    }
}

public class PageInfo
{
    public IDictionary<ProposalSourceEnum, int>  ProposalSkipCount { get; set; }
}