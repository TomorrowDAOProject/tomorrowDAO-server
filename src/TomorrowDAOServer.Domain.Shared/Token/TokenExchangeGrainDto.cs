using System.Collections.Generic;
using Orleans;

namespace TomorrowDAOServer.Token;

[GenerateSerializer]
public class TokenExchangeGrainDto
{
    [Id(0)] public long LastModifyTime { get; set; }
    [Id(1)] public long ExpireTime { get; set; }
    [Id(2)] public Dictionary<string, TokenExchangeDto> ExchangeInfos { get; set; } = new();
}