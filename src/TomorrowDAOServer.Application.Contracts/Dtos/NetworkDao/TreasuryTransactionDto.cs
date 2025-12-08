using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Dtos.NetworkDao;

public class TreasuryTransactionRequest : PagedResultRequestDto
{
    
    public TreasuryTransactionRequest() {}
    
    public TreasuryTransactionRequest(int skipCount, int maxResultCount)
    {
        base.SkipCount = skipCount;
        base.MaxResultCount = maxResultCount;
    }


    public string ChainId { get; set; }

}


public class TreasuryTransactionDto
{

    public string TransactionHash { get; set; }
    public string TransactionTime { get; set; }
    public string MethodName { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string InOrOut { get; set; }
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public TokenDto Token { get; set; }
    
}