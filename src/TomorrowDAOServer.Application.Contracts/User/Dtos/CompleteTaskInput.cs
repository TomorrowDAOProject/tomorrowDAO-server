using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.User.Dtos;

public class CompleteTaskInput
{
    [Required] public string ChainId { get; set; }
    public string UserTask { get; set; }
    public string UserTaskDetail { get; set; }
}