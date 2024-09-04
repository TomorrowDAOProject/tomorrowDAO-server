using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class RankingAppIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [PropertyName("DAOId")]
    [Keyword] public string DAOId { get; set; }
    [Keyword] public string ProposalId { get; set; }
    [Keyword] public string ProposalTitle { get; set; }
    [Keyword] public string ProposalDescription { get; set; }
    public DateTime ActiveStartTime { get; set; }
   
    public DateTime ActiveEndTime { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string Alias { get; set; }
    [Keyword] public string Title { get; set; }
    [Keyword] public string Icon { get; set; }
    [Keyword] public string Description { get; set; }
    public bool EditorChoice { get; set; }
    public DateTime DeployTime { get; set; }
    public long VoteAmount { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
}