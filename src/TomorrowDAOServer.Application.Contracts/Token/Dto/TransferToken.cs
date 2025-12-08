using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Common.Enum;

namespace TomorrowDAOServer.Token.Dto;

public class TransferTokenInput
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
}

public class TransferTokenStatusInput : TransferTokenInput
{
    public string Address { get; set; }
}

public class TransferTokenResponse
{
    public TransferTokenStatus Status { get; set; }
}

public class TokenClaimRecord
{
    public string TransactionId { get; set; }
    public string ClaimTime { get; set; }
    public TransferTokenStatus Status { get; set; }
    public bool IsClaimedInSystem { get; set; }
}