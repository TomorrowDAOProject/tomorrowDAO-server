using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Serilog;
using TomorrowDAOServer.Vote.Index;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleGetVoteItemsAsync(Exception e, string chainId, List<string> votingItemIds)
    {
        Log.Error(e, "GetVoteItemsAsync Exception chainId {chainId}, votingItemIds {votingItemIds}", chainId,
            votingItemIds);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new Dictionary<string, IndexerVote>()
        };
    }
}