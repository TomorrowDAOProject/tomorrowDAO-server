namespace TomorrowDAOServer.Token;

public class UserTokenDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public string ImageUrl { get; set; }
    public int Decimals { get; set; }
    public long Balance { get; set; }
}