using Newtonsoft.Json;

namespace TwitterListenerGAgent.GAgents.Features.Dtos;

[GenerateSerializer]
public class UserPostsResponseDto
{
    [JsonProperty("data")]
    [Id(0)] public List<Tweet> Data { get; set; }
    
    [JsonProperty("meta")]
    [Id(1)] public Meta Meta { get; set; }
    
    [JsonProperty("includes")]
    [Id(2)] public Includes Includes { get; set; }
}

[GenerateSerializer]
public class Tweet
{
    [JsonProperty("id")]
    [Id(0)] public string Id { get; set; }
    
    [JsonProperty("text")]
    [Id(1)] public string Text { get; set; }
    
    [JsonProperty("author_id")]
    [Id(2)] public string AuthorId { get; set; }
    
    [JsonProperty("created_at")]
    [Id(3)] public DateTime CreatedAt { get; set; }
    
    [JsonProperty("username")]
    [Id(4)] public string UserName { get; set; }
}

[GenerateSerializer]
public class Meta
{
    [JsonProperty("result_count")]
    [Id(0)] public int ResultCount { get; set; }
    
    [JsonProperty("newest_id")]
    [Id(1)] public string NewestId { get; set; }
    
    [JsonProperty("oldest_id")]
    [Id(2)] public string OldestId { get; set; }
}

[GenerateSerializer]
public class Includes
{
    [Id(0)] public List<IncludesPlace> Places { get; set; }
    [Id(1)] public List<IncludesTopic> Topics { get; set; }
    [Id(2)] public List<IncludesUser> Users { get; set; }
}

[GenerateSerializer]
public class IncludesPlace
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Name { get; set; }
    [JsonProperty("place_type")]
    [Id(2)] public string PlaceType { get; set; }
    [Id(3)] public string Country { get; set; }
    [JsonProperty("country_code")]
    [Id(4)] public string CountryCode { get; set; }
    [JsonProperty("full_name")]
    [Id(5)] public string FullName { get; set; }
}

[GenerateSerializer]
public class IncludesTopic
{
    [Id(0)] public string Description { get; set; }
    [Id(1)] public string Id { get; set; }
    [Id(2)] public string Name { get; set; }
}

[GenerateSerializer]
public class IncludesUser
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Name { get; set; }
    [JsonProperty("username")]
    [Id(2)] public string UserName { get; set; }
    [JsonProperty("created_at")]
    [Id(3)] public string CreatedAt { get; set; }
    [Id(4)] public string Description { get; set; }
}

