using System.Collections.Generic;

namespace TomorrowDAOServer.Referral.Indexer;

public class IndexerCaHolderTransaction
{
    public CaHolderTransaction CaHolderTransaction { get; set; }
}

public class CaHolderTransaction
{
    public List<CaHolderTransactionDetail> Data { get; set; }
}

public class CaHolderTransactionDetail
{
    public long Timestamp { get; set; }
}