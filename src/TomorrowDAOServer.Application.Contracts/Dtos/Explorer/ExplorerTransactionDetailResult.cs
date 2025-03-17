using System.Collections.Generic;

namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerTransactionDetailResult
{
    public string TransactionId { get; set; }
    public int Status { get; set; }
    public string Method { get; set; }
    public TransactionDetailAddress From { get; set; }
    public TransactionDetailAddress To { get; set; }
    public string TransactionParams { get; set; }
    public List<TransferredDetail> TokenTransferreds { get; set; }
}

public class TransactionDetailAddress
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int AddressType { get; set; }
    public bool IsManager { get; set; }
    public bool IsProducer { get; set; }
    public string ChainId { get; set; }
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