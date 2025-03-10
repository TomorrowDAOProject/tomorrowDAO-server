using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.Telegram.Dto;

public class AppDetailDto
{
    public string Id { get; set; }
    public string Alias { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
    public List<string> Categories { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public string AppType { get; set; }
    public string Creator { get; set; }
    public DateTime LoadTime { get; set; }
    public long TotalPoints { get; set; }
    public long TotalVotes { get; set; }
    public long TotalLikes { get; set; }
    public long TotalComments { get; set; }
    public long TotalOpens { get; set; }
    public long TotalShares { get; set; }
}
