using System.Collections.Generic;
using TomorrowDAOServer.Vote.Dto;

namespace TomorrowDAOServer.Proposal.Dto;

public class ProposalDetailDto : ProposalDto
{
    public List<ProposalLifeDto> ProposalLifeList { get; set; }
    public List<VoteRecordDto> VoteTopList { get; set; }
    public bool CanExecute { get; set; }
}