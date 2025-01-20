using System.Collections.Generic;
using TomorrowDAOServer.Enums;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Telegram.Dto;

public class GetTelegramAppInput : PagedResultRequestDto
{
    public string ChainId { get; set; }
    public List<string> Names { get; set; }
    public List<string> Aliases { get; set; }
    public List<SourceType> SourceTypes { get; set; }
}

public class GetTelegramAppResultDto : PagedResultDto<TelegramAppDisplayDto>
{
    
}