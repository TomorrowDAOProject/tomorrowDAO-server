using System.Collections.Generic;

namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerTransactionDetailResult
{
    public string TransactionId { get; set; }
    public List<TransferredDetail> TokenTransferreds { get; set; }
}

public class TransferredDetail
{
    public string Symbol { get; set; }
    public AddressDetail From { get; set; }
    public AddressDetail To { get; set; }
}

public class AddressDetail
{
    public string Name { get; set; }
    public string Address { get; set; }
}