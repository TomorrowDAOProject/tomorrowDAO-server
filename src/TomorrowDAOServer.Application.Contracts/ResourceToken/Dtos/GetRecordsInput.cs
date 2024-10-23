namespace TomorrowDAOServer.ResourceToken.Dtos;

public class GetRecordsInput
{
    public int Limit { get; set; }
    public int Page { get; set; }
    public string Order { get; set; }
    public string Address { get; set; }
}