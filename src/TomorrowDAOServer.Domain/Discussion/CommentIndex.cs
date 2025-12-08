using AElf.Indexing.Elasticsearch;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.Discussion;

public class CommentIndex : AbstractEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string DAOId { get; set; }
    [Keyword] public string ProposalId { get; set; }
    [Keyword] public string Commenter { get; set; } //address
    [Keyword] public string CommenterId { get; set; } //userId
    [Keyword] public string CommenterName { get; set; } //userName
    public string CommenterFirstName { get; set; }
    public string CommenterLastName { get; set; }
    public string CommenterPhoto { get; set; }
    [Keyword] public string Deleter { get; set; }
    [Keyword] public string DeleterId { get; set; } //userId
    [Keyword] public string DeleterName { get; set; } //userName
    public string DeleterFirstName { get; set; }
    public string DeleterLastName { get; set; }
    public string DeleterPhoto { get; set; }
    [Keyword] public string Comment { get; set; }
    [Keyword] public string ParentId { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public CommentStatusEnum CommentStatus { get; set; }
    public long CreateTime { get; set; }
    public long ModificationTime { get; set; }
}