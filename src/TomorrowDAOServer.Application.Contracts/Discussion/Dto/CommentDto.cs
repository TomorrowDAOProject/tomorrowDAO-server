namespace TomorrowDAOServer.Discussion.Dto;

public class CommentDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string DAOId { get; set; }
    public string ProposalId { get; set; }
    public string Alias { get; set; }
    public string Commenter { get; set; }
    public string CommenterId { get; set; }
    public string CommenterName { get; set; }
    public string CommenterFirstName { get; set; }
    public string CommenterLastName { get; set; }
    public string CommenterPhoto { get; set; }
    public string Comment { get; set; }
    public string ParentId { get; set; }
    public string CommentStatus { get; set; }
    public long CreateTime { get; set; }
    public long ModificationTime { get; set; }
}