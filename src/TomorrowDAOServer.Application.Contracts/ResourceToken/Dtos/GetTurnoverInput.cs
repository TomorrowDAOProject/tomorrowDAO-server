namespace TomorrowDAOServer.ResourceToken.Dtos;

public class GetTurnoverInput
{
    public int Range { get; set; }
    public int TimeZone { get; set; }
    public int Interval { get; set; }
    public string Type { get; set; }
}