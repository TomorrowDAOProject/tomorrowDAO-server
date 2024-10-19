using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace TomorrowDAOServer.Entities;

public class FileUploadIndex: AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string Url { get; set; }
    [Keyword] public string Uploader { get; set; }
    public DateTime CreateTime { get; set; }
    public bool Deleted { get; set; }
}