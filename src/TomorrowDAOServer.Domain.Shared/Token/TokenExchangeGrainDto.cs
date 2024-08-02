using System.Collections.Generic;

namespace TomorrowDAOServer.Token;

public class TokenExchangeGrainDto
{
    public long LastModifyTime { get; set; }
    public long ExpireTime { get; set; }
    public Dictionary<string, TokenExchangeDto> ExchangeInfos { get; set; } = new();
}