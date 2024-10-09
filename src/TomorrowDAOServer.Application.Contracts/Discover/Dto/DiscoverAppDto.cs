using System.Collections.Generic;

namespace TomorrowDAOServer.Discover.Dto;

public class DiscoverAppDto
{
    public string Alias { get; set; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public bool EditorChoice { get; set; }
    public string Url { get; set; }
    public string LongDescription { get; set; }
    public List<string> Screenshots { get; set; }
    public long TotalPoints { get; set; }
    public List<string> Categories { get; set; }
}