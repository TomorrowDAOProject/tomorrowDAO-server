using Newtonsoft.Json;

namespace TomorrowDAOServer.Auth.Dtos;

public class TelegramAuthDataDto
{
    public string Id { get; set; }
    public string UserName { get; set; }
    [JsonProperty("auth_date")] public string AuthDate { get; set; }
    [JsonProperty("first_name")] public string FirstName { get; set; }
    [JsonProperty("last_name")] public string LastName { get; set; }
    public string Hash { get; set; }
    [JsonProperty("photo_url")] public string PhotoUrl { get; set; }
    [JsonProperty("bot_id")] public string BotId { get; set; }
}