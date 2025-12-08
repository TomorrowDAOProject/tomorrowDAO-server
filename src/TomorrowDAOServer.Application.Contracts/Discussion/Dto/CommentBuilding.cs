using System.Collections.Generic;

namespace TomorrowDAOServer.Discussion.Dto;

public class CommentBuilding
{
    public List<CommentBuilding> SubComments { get; set; } = new();
    public string Id { get; set; }
    public CommentDto Comment { get; set; }
}