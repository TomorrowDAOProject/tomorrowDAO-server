namespace TomorrowDAOServer.Dtos.Explorer;

public class ExplorerBaseResponse<T>
{
    public int Code { get; set; }
    public string Msg { get; set; }
    public T Data { get; set; }
    public bool Success => Code is 0 or 20000;
    
}