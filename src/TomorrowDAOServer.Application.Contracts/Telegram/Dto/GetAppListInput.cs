namespace TomorrowDAOServer.Telegram.Dto;

public class GetAppListInput
{
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 6;
}