using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerTransactionRequest: ExplorerPagerRequest
{

    public ExplorerTransactionRequest() {}
    
    
    public ExplorerTransactionRequest(PagedResultRequestDto pageRequest)
    {
        PageNum = pageRequest.SkipCount / pageRequest.MaxResultCount;
        PageSize = pageRequest.MaxResultCount;
    }
    
    public string Address { get; set; }
    public string Order { get; set; } = "DESC";

}


public class ExplorerTransactionResponse
{
    public string Id { get; set;}
    public string TxId { get; set;}
    public string From { get; set;}
    public string To { get; set;}
    public string Amount { get; set;}
    public string Symbol { get; set;}
    public string Action { get; set;}
    public string IsCrossChain { get; set;}
    public string RelatedChainId { get; set;}
    public string Memo { get; set;}
    public string TxFee { get; set;}
    public string Time { get; set;}
    public string Method { get; set;}
    public string BlockHeight { get; set;}
    public string AddressFrom { get; set;}

}