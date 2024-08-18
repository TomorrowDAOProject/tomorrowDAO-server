using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Ranking.Dto;

namespace TomorrowDAOServer.Ranking;

public interface IRankingAppService
{
    Task GenerateRankingApp(List<IndexerProposalDto> proposalList);
    Task<RankingResultDto> GetDefaultProposalAsync(string chainId);
}