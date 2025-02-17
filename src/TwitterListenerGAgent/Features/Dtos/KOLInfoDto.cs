namespace TwitterListenerGAgent.GAgents.Features.Dtos;

[GenerateSerializer]
public class KOLInfoDto
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public string UserName { get; set; }
}

[GenerateSerializer]
public class LookupByUserNameResponse
{
    [Id(0)] public List<KOLInfoDto> Data { get; set; }
}