namespace TomorrowDAOServer.User.Dtos;

public class CollectLoginPointsInput
{
    public string ChainId { get; set; }
    public string Signature { get; set; }
    public string TimeStamp { get; set; }
}