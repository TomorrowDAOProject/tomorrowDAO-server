using Google.Type;

namespace TomorrowDAOServer.ChainFm.Dtos;

public class SmartMoneyDto
{
    public virtual string Id { get; set; }
    public string Address { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public SmartMoneySourceEnum Source { get; set; }
}