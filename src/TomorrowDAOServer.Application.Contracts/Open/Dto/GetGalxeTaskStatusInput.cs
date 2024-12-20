using AElf.Types;

namespace TomorrowDAOServer.Open.Dto;

public class GetGalxeTaskStatusInput
{
    public string Address { get; set; }
    public string TelegramId { get; set; }
}

public class GetGalxeTaskStatusDto
{
    public string TelegramId { get; set; }
    public string Address { get; set; }
    public long VoteCount { get; set; }
}