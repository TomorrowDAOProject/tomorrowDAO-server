using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Ranking.Dto;

namespace TomorrowDAOServer.Ranking;

public interface IRankingAppService
{
    Task GenerateRankingApp(string chainId, List<IndexerProposal> proposalList);
    Task<RankingDetailDto> GetDefaultRankingProposalAsync(string chainId);
    Task<PageResultDto<RankingListDto>> GetRankingProposalListAsync(GetRankingListInput input);
    Task<RankingDetailDto> GetRankingProposalDetailAsync(string address, string chainId, string proposalId);
    Task<RankingDetailDto> GetRankingProposalDetailAsync(string chainId, string proposalId);
    Task<RankingVoteRecord> GetRankingVoteRecordAsync(string chainId, string address, string proposalId);
    Task<RankingVoteResponse> VoteAsync(RankingVoteInput input);
    Task<RankingVoteRecord> GetVoteStatusAsync(GetVoteStatusInput input);
    Task MoveHistoryDataAsync(string chainId, string type, string key, string value);
    Task<long> LikeAsync(RankingAppLikeInput input);
    Task<RankingActivityResultDto> GetRankingActivityResultAsync(string chainId, string proposalId, int count);
}