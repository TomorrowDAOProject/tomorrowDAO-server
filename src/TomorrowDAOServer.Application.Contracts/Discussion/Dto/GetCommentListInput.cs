namespace TomorrowDAOServer.Discussion.Dto;

public class GetCommentListInput : CommentBaseInput
{
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 6;
    public string SkipId { get; set; } = string.Empty;
}