using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Ranking.Dto;

namespace TomorrowDAOServer.MQ;

public interface IMessagePublisherService
{
    Task SendLikeMessageAsync(string chainId, string proposalId, string address, List<RankingAppLikeDetailDto> likeList,
        string userId = "");

    Task SendVoteMessageAsync(string chainId, string proposalId, string address, string appAlias, long amount);
    Task SendReferralFirstVoteMessageAsync(string chainId, string inviter, string invitee);
    Task SendOpenMessageAsync(string chainId, string address, string userId, string appAlias, long count);
    Task SendSharedMessageAsync(string chainId, string address, string userId, string alias, int count);
}