namespace TomorrowDAOServer.Auth.Options;

public class TelegramAuthOptions
{
    public string PortkeyUrl { get; set; }
    public int Expire { get; set; } = 60 * 60; //s
    public string BotToken { get; set; }
}