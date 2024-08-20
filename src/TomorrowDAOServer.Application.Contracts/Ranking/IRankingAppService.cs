using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Ranking.Dto;

namespace TomorrowDAOServer.Ranking;

public interface IRankingAppService
{
    Task GenerateRankingApp(List<IndexerProposal> proposalList);
    Task<RankingDetailDto> GetDefaultRankingProposalAsync(string chainId);
    Task<PageResultDto<RankingListDto>> GetRankingProposalListAsync(GetRankingListInput input);
    Task<RankingDetailDto> GetRankingProposalDetailAsync(string chainId, string proposalId, string daoId);
    Task<RankingVoteRecord> GetRankingVoteRecordAsync(string chainId, string address, string proposalId);
    Task<RankingVoteResponse> VoteAsync(RankingVoteInput input);
    Task<RankingVoteRecord> GetVoteStatusAsync(GetVoteStatusInput input);
}