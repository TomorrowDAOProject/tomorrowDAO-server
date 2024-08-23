using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Common;

public static class MapHelper
{
    public static T MapJsonConvert<T>(string jsonString)
    {
        return JsonConvert.DeserializeObject<T>(jsonString);
    }
    
    public static ProposalStatus? MapProposalStatus(ProposalStatus? realProposalStatus)
    {
        return realProposalStatus switch
        {
            ProposalStatus.Rejected or ProposalStatus.Abstained or ProposalStatus.BelowThreshold => ProposalStatus.Defeated,
            _ => realProposalStatus
        };
    }

    public static string MapProposalStatusString(ProposalStatus realProposalStatus)
    {
        return MapProposalStatus(realProposalStatus).ToString();
    }
    
    public static string MapProposalStageString(ProposalStage realProposalStage)
    {
        return realProposalStage switch
        {
            ProposalStage.Execute => "Queued",
            _ => realProposalStage.ToString()
        };
    }
    
    public static Dictionary<string, string> ToDictionary(object param)
    {
        switch (param)
        {
            case null:
                return null;
            case Dictionary<string, string> dictionary:
                return dictionary;
            default:
            {
                var json = param as string ?? JsonConvert.SerializeObject(param, JsonSettingsBuilder.New()
                    .WithCamelCasePropertyNamesResolver().IgnoreNullValue().Build());
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
        }
    }

    public static string MapAlias(string memo, string alias, bool validRankingVote)
    {
        try
        {
            if (!validRankingVote)
            {
                return string.Empty;
            }
        
            if (!string.IsNullOrEmpty(alias))
            {
                return alias;
            }

            return Regex.Match(memo, CommonConstant.MemoPattern).Groups[1].Value;
        }
        catch (Exception)
        {
            return string.Empty;
        }
        
    }
}