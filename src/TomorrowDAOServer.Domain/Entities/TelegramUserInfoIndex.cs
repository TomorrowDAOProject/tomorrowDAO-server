using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class TelegramUserInfoIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string Icon { get; set; }
    [Keyword] public string FirstName { get; set; }
    [Keyword] public string LastName { get; set; }
    [Keyword] public string UserName { get; set; }
    public DateTime UpdateTime { get; set; }
}