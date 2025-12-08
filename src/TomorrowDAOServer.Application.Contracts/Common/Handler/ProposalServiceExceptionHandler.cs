using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using TomorrowDAOServer.Proposal.Dto;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleGetProposalListAsync(Exception e, QueryProposalListInput input)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new Tuple<long, List<ProposalDto>>(0, new List<ProposalDto>())
        };
    }
}