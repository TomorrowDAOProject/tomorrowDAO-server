namespace TomorrowDAOServer.Discussion.Dto;

public class NewCommentResultDto
{
    public bool Success { get; set; }
    public string Reason { get; set; } = string.Empty;
    public CommentIndex Comment { get; set; } = new();
}