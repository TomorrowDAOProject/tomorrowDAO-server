using System.Collections.Generic;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Proposal.Index;

namespace TomorrowDAOServer.Common;

public class InformationHelper
{
    public static Dictionary<string, string> GetDailyCreatePollInformation(IndexerProposal proposal)
    {
        if (proposal == null)
        {
            return new Dictionary<string, string>();
        }
        return new Dictionary<string, string>
        {
            { CommonConstant.ProposalDescription, proposal.ProposalDescription },
            { CommonConstant.ProposalId, proposal.ProposalId },
            { CommonConstant.ProposalTitle, proposal.ProposalTitle },
        };
    }

    public static Dictionary<string, string> GetTopInviterInformation(long startTime, long endTime, long rank, long inviteCount)
    {
        return new Dictionary<string, string>
        {
            { CommonConstant.CycleStartTime, startTime.ToString() },
            { CommonConstant.CycleEndTime, endTime.ToString() },
            { CommonConstant.Rank, rank.ToString() },
            { CommonConstant.InviteCount, inviteCount.ToString() }
        };
    }
    
    public static Dictionary<string, string> GetDailyVoteInformation(ProposalIndex proposalIndex, string alias)
    {
        return new Dictionary<string, string>
        {
            { CommonConstant.ProposalId, proposalIndex?.ProposalId ?? string.Empty },
            { CommonConstant.ProposalTitle, proposalIndex?.ProposalTitle ?? string.Empty },
            { CommonConstant.Alias, alias}
        };
    }

    public static Dictionary<string, string> GetBeInviteVoteInformation(string inviter)
    {
        return new Dictionary<string, string>
        {
            { CommonConstant.Inviter, inviter }
        };
    }
    
    public static Dictionary<string, string> GetInviteVoteInformation(string invitee)
    {
        return GetDailyFirstInviteInformation(invitee);
    }

    public static Dictionary<string, string> GetDailyFirstInviteInformation(string invitee)
    {
        return new Dictionary<string, string>
        {
            { CommonConstant.Invitee, invitee }
        };
    }
    
    public static Dictionary<string, string> GetViewAdInformation(string adPlatform, long timeStamp)
    {
        return new Dictionary<string, string>
        {
            { CommonConstant.AdPlatform, adPlatform },
            { CommonConstant.AdTime, timeStamp.ToString() }
        };
    }
}