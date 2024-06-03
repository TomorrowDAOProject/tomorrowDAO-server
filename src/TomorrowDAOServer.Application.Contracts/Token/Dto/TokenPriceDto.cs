namespace TomorrowDAOServer.Token.Dto;

public class TokenPriceDto
{
    public string BaseCoin { get; set; }
    public string QuoteCoin { get; set; }
    public decimal Price { get; set; }
}