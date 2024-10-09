using System.Collections.Generic;

namespace TomorrowDAOServer.NetworkDao.Migrator.ES;

public class GetProposalInfoInput
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string ProposalId { get; set; }
}

public class GetProposalInfoResultDto
{
    public GetProposalListResultDto Proposal { get; set; }
    public List<string> BpList { get; set; }
    public GetProposalListResultDto.OrganizationInfoDto Organization { get; set; }
    public List<string> ParliamentProposerList { get; set; }
}