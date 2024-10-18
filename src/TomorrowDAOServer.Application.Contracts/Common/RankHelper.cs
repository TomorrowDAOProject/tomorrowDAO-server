using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    public static bool IsRanking(string description, string pattern = null)
    {
        var isMatch = false;
        if (!pattern.IsNullOrWhiteSpace())
        {
            isMatch = Regex.IsMatch(description, pattern!);
        }

        if (!isMatch)
        {
            isMatch = Regex.IsMatch(description, CommonConstant.NewDescriptionPattern);
            if (!isMatch)
            {
                isMatch = Regex.IsMatch(description, CommonConstant.OldDescriptionPattern);
            }
        }
        
        return isMatch;
    }

    public static List<string> GetAliases(string description)
    {
        var match = Regex.Match(description, CommonConstant.NewDescriptionPattern);
        if (match.Success)
        {
            var aList = new List<string>();
            var aliasString = match.Groups[1].Value;
            var aliasMatches = Regex.Matches(aliasString, CommonConstant.NewDescriptionAliasPattern);
            foreach (Match aliasMatch in aliasMatches)
            {
                aList.Add(aliasMatch.Groups[1].Value.Trim());
            }
            
            return aList.Distinct().ToList();
        }
        else
        {
            match = Regex.Match(description, CommonConstant.OldDescriptionPattern);
            if (match.Success)
            {
                var aliasString = match.Groups[1].Value;
                return aliasString.Split(CommonConstant.Comma).Select(t => t.Trim()).Distinct().ToList();
            }
        }

        return new List<string>();
    }
    
    public static string GetAliasString(string description)
    {
        var match = Regex.Match(description, CommonConstant.NewDescriptionPattern);
        if (match.Success)
        {
            var aList = new List<string>();
            var aliasString = match.Groups[1].Value;
            var aliasMatches = Regex.Matches(aliasString, CommonConstant.NewDescriptionAliasPattern);
            foreach (Match aliasMatch in aliasMatches)
            {
                aList.Add(aliasMatch.Groups[1].Value.Trim());
            }
            return string.Join(CommonConstant.Comma, aList.Distinct());
        }
        else
        {
            match = Regex.Match(description, CommonConstant.OldDescriptionPattern);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }
        return string.Empty;
    }

    public static string BuildProposalDescription(List<string> aliases, string banner)
    {
        if (aliases.IsNullOrEmpty())
        {
            return string.Empty;
        }

        var builder = new StringBuilder(CommonConstant.DescriptionBegin);
        foreach (var alias in aliases)
        {
            builder.Append(CommonConstant.LeftParenthesis).Append(alias).Append(CommonConstant.RightParenthesis).Append(CommonConstant.Comma);
        }

        builder.Length = builder.Length - 1;
        builder.Append(CommonConstant.DescriptionIconBegin).Append(CommonConstant.LeftParenthesis).Append(banner)
            .Append(CommonConstant.RightParenthesis);
        return builder.ToString();
    }

    public static string GetBanner(string description)
    {
        var match = Regex.Match(description, CommonConstant.NewDescriptionPattern);
        if (match.Success)
        {
            return match.Groups[2].Value?.Trim();
        }

        return string.Empty;
    }

    public static List<string> GetAliasList(string description)
    {
        return description.Replace(CommonConstant.DescriptionBegin, CommonConstant.EmptyString)
            .Trim().Split(CommonConstant.Comma).Select(alias => alias.Trim()).Distinct().ToList();
    }
}