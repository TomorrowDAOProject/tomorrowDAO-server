using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Ranking.Dto;

namespace TomorrowDAOServer.MQ;

public interface IMessagePublisherService
{
    Task SendLikeMessageAsync(string chainId, string proposalId, string address, List<RankingAppLikeDetailDto> likeList,
        string userId = "", Dictionary<string, long> addedAliasDic = null);

    Task SendVoteMessageAsync(string chainId, string proposalId, string address, string appAlias, long amount, bool dailyVote = false);
    Task SendReferralFirstVoteMessageAsync(string chainId, string inviter, string invitee);
    Task SendOpenMessageAsync(string chainId, string address, string userId, string appAlias, long count);
}