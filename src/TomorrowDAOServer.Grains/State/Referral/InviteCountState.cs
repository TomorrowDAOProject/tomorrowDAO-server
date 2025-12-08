namespace TomorrowDAOServer.Grains.State.Referral;

[GenerateSerializer]
public class InviteCountState
{
    [Id(0)] public long InviteCount { get; set; }
}