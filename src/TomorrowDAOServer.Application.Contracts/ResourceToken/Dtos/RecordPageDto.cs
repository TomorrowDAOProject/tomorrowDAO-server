using System.Collections.Generic;

namespace TomorrowDAOServer.ResourceToken.Dtos;

public class RecordPageDto
{
    public long Total { get; set; }
    public List<RecordDto> Records { get; set; }
}