using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.ResourceToken.Dtos;

namespace TomorrowDAOServer.ResourceToken;

public interface IResourceTokenService
{
    Task<RealtimeRecordsDto> GetRealtimeRecordsAsync(int limit, string type);
    Task<List<TurnoverDto>> GetTurnoverAsync(GetTurnoverInput input);
    Task<RecordPageDto> GetRecordsAsync(GetRecordsInput input);
}