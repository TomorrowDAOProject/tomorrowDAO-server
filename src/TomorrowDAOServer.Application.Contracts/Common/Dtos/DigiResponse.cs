namespace TomorrowDAOServer.Common.Dtos;

public class DigiResponse
{
    private const long SuccessCode = 30024;
    public long Code { get; set; }
    public bool Success => Code == SuccessCode;
}