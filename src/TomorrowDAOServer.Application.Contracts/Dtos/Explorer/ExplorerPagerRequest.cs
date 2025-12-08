namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerPagerRequest
{
    
    // PageSize-PageNum pair
    public int PageSize { get; set; } = 10;
    public int PageNum { get; set; } = 1;
    

    // Limit-Page pair
    public int Limit => PageSize;
    public int Page => PageNum - 1;
    
    

    protected ExplorerPagerRequest()
    {
    }

    protected ExplorerPagerRequest(int pageNum, int pageSize)
    {
        PageNum = pageNum;
        PageSize = pageSize;
    }
    
    
}