using System.Threading.Tasks;
using TomorrowDAOServer.ResourceToken.Dtos;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace TomorrowDAOServer.ResourceToken;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ResourceTokenService : TomorrowDAOServerAppService, IResourceTokenService
{
    public Task<RealtimeRecordsDto> GetRealtimeRecordsAsync(int limit, string type)
    {
        throw new System.NotImplementedException();
    }

    public Task<TurnoverDto> GetTurnoverAsync(GetTurnoverInput input)
    {
        throw new System.NotImplementedException();
    }

    public Task<RecordPageDto> GetRecordsAsync(GetRecordsInput input)
    {
        throw new System.NotImplementedException();
    }
}