using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoOrgService : TomorrowDAOServerAppService, INetworkDaoOrgService
{
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly IScriptService _scriptService;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IObjectMapper _objectMapper;

    public NetworkDaoOrgService(INetworkDaoEsDataProvider networkDaoEsDataProvider, IScriptService scriptService,
        IGraphQLProvider graphQlProvider, IObjectMapper objectMapper)
    {
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _scriptService = scriptService;
        _graphQlProvider = graphQlProvider;
        _objectMapper = objectMapper;
    }

    public async Task<GetOrgOfOwnerListPagedResult> GetOrgOfOwnerListAsync(GetOrgOfOwnerListInput input)
    {
        var totalCount = 0L;
        var daoOrgIndices = new List<NetworkDaoOrgIndex>();
        if (input.ProposalType == NetworkDaoOrgType.Parliament)
        {
            var bpList = await _graphQlProvider.GetBPAsync(input.ChainId);
            var isBp = bpList.Contains(input.Address);
            if (isBp)
            {
                (totalCount, daoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
                {
                    MaxResultCount = input.MaxResultCount,
                    SkipCount = input.SkipCount,
                    Sorting = input.Sorting,
                    ChainId = input.ChainId,
                    OrgType = NetworkDaoOrgType.Parliament
                });
            }
        }
        else if (input.ProposalType == NetworkDaoOrgType.Association)
        {
            var (orgMemberCount, orgMemberIndices) = await _networkDaoEsDataProvider.GetOrgMemberListAsync(
                new GetOrgMemberListInput
                {
                    MaxResultCount = input.MaxResultCount,
                    SkipCount = input.SkipCount,
                    Sorting = input.Sorting,
                    ChainId = input.ChainId,
                    OrgAddress = input.Search,
                    MemberAddress = input.Address
                });
            if (orgMemberIndices.IsNullOrEmpty())
            {
                var orgAddressList = orgMemberIndices.Select(t => t.OrgAddress).Distinct().ToList();
                (totalCount, daoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
                {
                    ChainId = input.ChainId,
                    OrgAddresses = orgAddressList
                });
            }
        }
        else if (input.ProposalType == NetworkDaoOrgType.Referendum)
        {
            (totalCount, daoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
            {
                MaxResultCount = input.MaxResultCount,
                SkipCount = input.SkipCount,
                Sorting = input.Sorting,
                ChainId = input.ChainId,
                OrgAddress = input.Search,
                OrgType = NetworkDaoOrgType.Referendum
            });
        }
        else
        {
            throw new UserFriendlyException("Invalid ProposalType!");
        }

        var resultDtos = _objectMapper.Map<List<NetworkDaoOrgIndex>, List<GetOrgOfOwnerListResultDto>>(daoOrgIndices);

        return new GetOrgOfOwnerListPagedResult
        {
            Items = resultDtos,
            TotalCount = totalCount
        };
    }
}