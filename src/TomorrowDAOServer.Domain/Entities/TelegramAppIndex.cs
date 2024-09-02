using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class TelegramAppIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Alias { get; set; }
    [Keyword] public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
}