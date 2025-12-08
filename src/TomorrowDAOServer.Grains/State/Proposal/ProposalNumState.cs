namespace TomorrowDAOServer.Grains.State.Proposal;

[GenerateSerializer]
public class ProposalNumState
{
    [Id(0)] public long ParliamentCount { get; set; }
    [Id(1)] public long AssociationCount { get; set; }
    [Id(2)] public long ReferendumCount { get; set; }
}