using TomorrowDAOServer.Enums;
using Volo.Abp.EventBus;

namespace TomorrowDAOServer.Ranking.Eto;

[EventName("VoteAndLikeMessageEto")]
public class VoteAndLikeMessageEto
{
    public string ChainId { get; set; }
    public string DaoId { get; set; }
    public string ProposalId { get; set; }
    public string AppId { get; set; }
    public string Alias { get; set; }
    public string Title { get; set; }
    public string Address { get; set; }
    public long Amount { get; set; }
    public PointsType PointsType { get; set; }
}