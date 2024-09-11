namespace TomorrowDAOServer.Common.Dtos;

public class InviteBoardPageResultDto<T> : PageResultDto<T>
{
    public T Me { get; set; }
}