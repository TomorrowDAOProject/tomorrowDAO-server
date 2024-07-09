namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerProposalListRequest : ExplorerPagerRequest
{
    
    public ExplorerProposalListRequest() {}
    
    public ExplorerProposalListRequest(int pageNum, int pageSize) : base(pageNum, pageSize)
    {
    }
    
    /// <see cref=" TomorrowDAOServer.Common.Enum.ProposalType"/>
    public string ProposalType { get; set; }
    
    /// <see cref=" TomorrowDAOServer.Common.Enum.ProposalStatusEnum"/>
    public string Status { get; set; }

    public int IsContract { get; set; }
    
    public string Address { get; set; }
    
    public string Search { get; set; }
    
}

public class ExplorerProposalResponse : ExplorerPagerResult<ExplorerProposalResult>
{
    public int BpCount { get; set; }
}