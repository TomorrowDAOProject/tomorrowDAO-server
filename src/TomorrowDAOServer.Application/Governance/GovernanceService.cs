using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Governance.Dto;
using TomorrowDAOServer.Governance.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Governance;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GovernanceService : TomorrowDAOServerAppService, IGovernanceService
{
    private readonly IObjectMapper _objectMapper;
    private readonly IGovernanceProvider _governanceProvider;

    public GovernanceService(IGovernanceProvider governanceProvider,
        IObjectMapper objectMapper)
    {
        _governanceProvider = governanceProvider;
        _objectMapper = objectMapper;
    }

    public async Task<GovernanceSchemeDto> GetGovernanceSchemeAsync(GetGovernanceSchemeListInput input)
    {
        var indexerGovernanceSchemeDto =await _governanceProvider.GetGovernanceSchemeAsync(input.ChainId, input.DaoId);
        return _objectMapper.Map<IndexerGovernanceSchemeDto, GovernanceSchemeDto>(indexerGovernanceSchemeDto);
    }
}