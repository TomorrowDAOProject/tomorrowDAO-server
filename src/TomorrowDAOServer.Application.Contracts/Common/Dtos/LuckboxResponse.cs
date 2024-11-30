namespace TomorrowDAOServer.Common.Dtos;

public class LuckboxResponse
{
    private const string SuccessCode = "10000";
    private const string FailCode = "400000";
    public string Code { get; set; }
    public string Msg { get; set; }
    public string Reason { get; set; }
    public bool Success => Code == SuccessCode;
}