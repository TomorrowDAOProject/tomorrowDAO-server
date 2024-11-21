using Newtonsoft.Json;

namespace TomorrowDAOServer.Common.Dtos;

public class LuckyboxRequest
{
    [JsonProperty("track_id")]
    public string TrackId { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}