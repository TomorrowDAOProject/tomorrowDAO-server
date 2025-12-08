using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.Election.Index;

public class HighCouncilManagedDaoIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword]
    public string MemberAddress { get; set; }
    [Keyword]
    public string DaoId { get; set; }
    [Keyword]
    public string ChainId { get; set; }
    public DateTime CreateTime { get; set; }
}