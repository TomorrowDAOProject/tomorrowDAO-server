namespace TomorrowDAOServer.User.Dtos;

public class UserBalanceProduceDto
{
    public string Symbol { get; set; }
    public string Address { get; set; }
    public long BeforeAmount { get; set; }
    public long NowAmount { get; set; }
}