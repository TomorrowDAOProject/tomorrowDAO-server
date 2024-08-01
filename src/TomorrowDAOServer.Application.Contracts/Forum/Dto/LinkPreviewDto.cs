using System.Collections.Generic;
using TomorrowDAOServer.Common.Enum;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Forum.Dto;

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