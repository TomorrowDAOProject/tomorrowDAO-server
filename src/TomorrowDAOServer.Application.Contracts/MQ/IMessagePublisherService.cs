using System.Collections.Generic;
using System.Threading.Tasks;

namespace TomorrowDAOServer.MQ;

public interface IMessagePublisherService
{
    Task SendLikeMessageAsync(string chainId, string proposalId, string address, IDictionary<string, long> appAmounts);

    Task SendVoteMessageAsync(string chainId, string proposalId, string address, string appAlias, long amount);
}