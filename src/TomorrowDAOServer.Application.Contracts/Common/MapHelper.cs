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
}