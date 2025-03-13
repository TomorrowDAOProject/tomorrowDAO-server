namespace TomorrowDAOServer.Dtos.AelfScan;

public class AelfScanBaseResponse<T>
{
    public string Code { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public bool Success => Code == "20000";
}