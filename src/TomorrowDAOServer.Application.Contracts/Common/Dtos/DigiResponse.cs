namespace TomorrowDAOServer.Common.Dtos;

public class DigiResponse
{
    private const long SuccessCode = 200;
    public long Code { get; set; }
    public bool Success => Code is SuccessCode or 30024;
}