namespace TomorrowDAOServer.Common.Dtos;

public class DigiResponse
{
    private const long SuccessCode = 200;
    public long Code { get; set; }
    public bool Success => Code == SuccessCode;
}