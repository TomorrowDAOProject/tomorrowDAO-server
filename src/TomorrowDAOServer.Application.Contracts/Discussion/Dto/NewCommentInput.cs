using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.Discussion.Dto;

public class NewCommentInput : CommentBaseInput
{
    [Required] public string Comment { get; set; }
}