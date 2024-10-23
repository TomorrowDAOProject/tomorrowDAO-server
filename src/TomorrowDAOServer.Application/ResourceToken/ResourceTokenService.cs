using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.ResourceToken.Dtos;
using TomorrowDAOServer.ResourceToken.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.ResourceToken;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ResourceTokenService : TomorrowDAOServerAppService, IResourceTokenService
{
    private readonly IResourceTokenProvider _resourceTokenProvider;
    private readonly IObjectMapper _objectMapper;

    public ResourceTokenService(IResourceTokenProvider resourceTokenProvider, IObjectMapper objectMapper)
    {
        _resourceTokenProvider = resourceTokenProvider;
        _objectMapper = objectMapper;
    }

    public async Task<RealtimeRecordsDto> GetRealtimeRecordsAsync(int limit, string type)
    {
        var buyList = await _resourceTokenProvider.GetLatestAsync(limit, type, CommonConstant.BuyMethod);
        var sellList = await _resourceTokenProvider.GetLatestAsync(limit, type, CommonConstant.SellMethod);
        return new RealtimeRecordsDto
        {
            BuyRecords = _objectMapper.Map<List<ResourceTokenIndex>, List<RecordDto>>(buyList),
            SellRecords = _objectMapper.Map<List<ResourceTokenIndex>, List<RecordDto>>(sellList)
        };
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