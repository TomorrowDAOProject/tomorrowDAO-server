using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Proposal.Index;

namespace TomorrowDAOServer.Ranking;

public interface IRankingAppService
{
    Task GenerateRankingApp(List<IndexerProposalDto> proposalList);
}