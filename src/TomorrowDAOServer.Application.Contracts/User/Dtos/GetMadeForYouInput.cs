namespace TomorrowDAOServer.User.Dtos;

public class GetMadeForYouInput
{
    public string ChainId { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } 
}

public class MadeForYouResultDto
{
    public long TotalCount { get; set; }
    
}