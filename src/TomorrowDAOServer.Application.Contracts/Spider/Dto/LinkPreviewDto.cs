using TomorrowDAOServer.Common.Enum;

namespace TomorrowDAOServer.Spider.Dto;

public class LinkPreviewDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Favicon { get; set; }
}

public class LinkPreviewInput
{
    public string ProposalId { get; set; }
    public string ChainId { get; set; }
    public string ForumUrl { get; set; }

    public AnalyzerType AnalyzerType { get; set; } = AnalyzerType.HtmlAgilityPack;
}