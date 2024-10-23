using System.Collections.Generic;

namespace TomorrowDAOServer.ResourceToken.Dtos;

public class RealtimeRecordsDto
{
    public List<RecordDto> BuyRecords { get; set; }
    public List<RecordDto> SellRecords { get; set; }
}