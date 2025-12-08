using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Common;

public static class MapHelper
{
    public static T MapJsonConvert<T>(string jsonString)
    {
        return JsonConvert.DeserializeObject<T>(jsonString);
    }
    
    public static Dictionary<string, string> MapConvertToStringDictionary (string jsonString)
    {
        if (jsonString.IsNullOrWhiteSpace())
        {
            return new Dictionary<string, string>();
        }
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
    }
    
    public static ProposalStatus? MapProposalStatus(ProposalStatus? realProposalStatus)
    {
        return realProposalStatus switch
        {
            ProposalStatus.Rejected or ProposalStatus.Abstained or ProposalStatus.BelowThreshold => ProposalStatus.Defeated,
            _ => realProposalStatus
        };
    }

    public static string MapProposalStatusString(ProposalIndex index)
    {
        var realProposalStatus = index.ProposalStatus;
        return DateTime.UtcNow < index.ActiveStartTime 
            ? ProposalStatus.Published.ToString() 
            : MapProposalStatus(realProposalStatus).ToString();
    }
    
    public static string MapProposalStageString(ProposalIndex index)
    {
        var realProposalStage = index.ProposalStage;
        return DateTime.UtcNow < index.ActiveStartTime 
            // ? ProposalStage.WaitingActive.ToString() 
            ? "Waiting Active"
            : MapProposalStageString(realProposalStage);
    }
    
    public static string MapProposalStageString(ProposalStage realProposalStage)
    {
        return realProposalStage switch
        {
            ProposalStage.Execute => "Queued",
            ProposalStage.WaitingActive => "Waiting Active",
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
}