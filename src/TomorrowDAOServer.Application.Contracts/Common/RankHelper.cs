using System.Collections.Generic;
using System.Linq;
using Nest;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Referral.Dto;

namespace TomorrowDAOServer.Common;

public class RankHelper
{
    public static List<InviteLeaderBoardDto> GetRankedList(string chainId, IEnumerable<UserIndex> userList, 
        IEnumerable<KeyedBucket<string>> inviterBuckets)
    {
        long rank = 1;           
        long lastInviteCount = -1;  
        long currentRank = 1;
        var userDic = userList
            .Where(x => x.AddressInfos.Any(ai => ai.ChainId == chainId))
            .GroupBy(ui => ui.CaHash)
            .ToDictionary(
                group => group.Key,
                group => group.First().AddressInfos.First(ai => ai.ChainId == chainId)?.Address ?? string.Empty
            );

        return inviterBuckets.Where(bucket => !string.IsNullOrEmpty(bucket.Key)).Select((bucket, _) =>
        {
            var inviteCount = (long)(bucket.ValueCount("invite_count").Value ?? 0);
            if (inviteCount != lastInviteCount)
            {
                currentRank = rank;
                lastInviteCount = inviteCount;
            }
            var referralInvite = new InviteLeaderBoardDto
            {
                InviterCaHash = bucket.Key,
                Inviter = userDic.GetValueOrDefault(bucket.Key, string.Empty),
                InviteAndVoteCount = inviteCount,
                Rank = currentRank  
            };
            rank++;  
            return referralInvite;
        }).ToList();
    }
    
    public static string GetAliasString(string description)
    {
        return description.Replace(CommonConstant.DescriptionBegin, CommonConstant.EmptyString).Trim();
    }
}