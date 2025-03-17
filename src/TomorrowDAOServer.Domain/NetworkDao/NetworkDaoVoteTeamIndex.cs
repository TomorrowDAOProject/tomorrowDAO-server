using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.NetworkDao;

public class NetworkDaoVoteTeamIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword]
    public string ChainId { get; set; }
    [Keyword]
    public string PublicKey { get; set; }
    [Keyword]
    public string Address { get; set; }
    [Keyword]
    public string Name { get; set; }
    [Keyword]
    public string Avatar { get; set; }
    public string Intro { get; set; }
    [Keyword]
    public string TxId { get; set; }
    [Keyword]
    public bool IsActive { get; set; }
    public List<string> Socials { get; set; }
    public string OfficialWebsite { get; set; }
    public string Location { get; set; }
    public string Mail { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}