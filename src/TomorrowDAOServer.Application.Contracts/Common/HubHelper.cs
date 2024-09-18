namespace TomorrowDAOServer.Common;

public class HubHelper
{
    public static string GetPointsGroupName(string chainId)
    {
        return $"{chainId}_Group_Points";
    }
    
    public static string GetUserBalanceGroupName(string chainId)
    {
        return $"{chainId}_Group_UserBalance";
    }
}