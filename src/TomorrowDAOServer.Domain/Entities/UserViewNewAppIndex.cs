using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class UserViewNewAppIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    public List<string> AliasesList { get; set; }
}