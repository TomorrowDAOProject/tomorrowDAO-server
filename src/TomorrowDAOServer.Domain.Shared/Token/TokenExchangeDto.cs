using Orleans;

namespace TomorrowDAOServer.Token;

[GenerateSerializer]
public class TokenExchangeDto
{
    [Id(0)] public string FromSymbol { get; set; }
    [Id(1)] public string ToSymbol { get; set; }
    [Id(2)] public decimal Exchange { get; set; }
    [Id(3)] public long Timestamp { get; set; }
}