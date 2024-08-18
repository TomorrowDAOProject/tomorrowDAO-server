using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Proposal.Index;

namespace TomorrowDAOServer.Entities;

public class RankingAppIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ProposalId { get; set; }
    [Keyword] public string ProposalTitle { get; set; }
    [Keyword] public string ProposalDescription { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string Alias { get; set; }
    [Keyword] public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
    
    public void OfProposal(IndexerProposalDto proposal)
    {
        ProposalId = proposal.ProposalId;
        ProposalDescription = proposal.ProposalDescription;
    }
}