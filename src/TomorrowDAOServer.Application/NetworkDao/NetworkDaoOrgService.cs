using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common.Provider;
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
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IObjectMapper _objectMapper;

    public NetworkDaoOrgService(INetworkDaoEsDataProvider networkDaoEsDataProvider,
        IGraphQLProvider graphQlProvider, IObjectMapper objectMapper)
    {
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _graphQlProvider = graphQlProvider;
        _objectMapper = objectMapper;
    }

    public async Task<GetOrganizationsPagedResult> GetOrganizationsAsync(GetOrganizationsInput input)
    {
        var getOrgListInput = _objectMapper.Map<GetOrganizationsInput, GetOrgListInput>(input);
        var (totalCount, networkDaoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(getOrgListInput);
        var resultDtos = _objectMapper.Map<List<NetworkDaoOrgIndex>, List<GetOrganizationsResultDto>>(networkDaoOrgIndices);

        if (!resultDtos.IsNullOrEmpty())
        {
            var orgAddressList = networkDaoOrgIndices.Select(t => t.OrgAddress).ToList();
            var orgProposerWhiteListDictionaryTask = GetOrgProposerWhiteListDictionaryAsync(input, orgAddressList);
            var orgMemberDictionaryTask = GetOrgMemberDictionaryAsync(input, orgAddressList);

            var orgProposerDictionary = await orgProposerWhiteListDictionaryTask;
            var orgMemberDictionary = await orgMemberDictionaryTask;

            foreach (var resultDto in resultDtos)
            {
                resultDto.LeftOrgInfo ??= new LeftOrgInfo();
                if (orgProposerDictionary.ContainsKey(resultDto.OrgAddress))
                {
                    resultDto.LeftOrgInfo.ProposerWhiteList ??= new ProposerWhiteList();
                    resultDto.LeftOrgInfo.ProposerWhiteList.Proposers = orgProposerDictionary[resultDto.OrgAddress];
                }
                if (orgMemberDictionary.ContainsKey(resultDto.OrgAddress))
                {
                    resultDto.LeftOrgInfo.OrganizationMemberList ??= new OrganizationMemberList();
                    resultDto.LeftOrgInfo.OrganizationMemberList.OrganizationMembers =
                        orgMemberDictionary[resultDto.OrgAddress];
                }
            }
        }

        return new GetOrganizationsPagedResult
        {
            Items = resultDtos,
            TotalCount = totalCount
        };
    }
    
    public async Task<GetOrgOfOwnerListPagedResult> GetOrgOfOwnerListAsync(GetOrgOfOwnerListInput input)
    {
        var totalCount = 0L;
        var daoOrgIndices = new List<NetworkDaoOrgIndex>();
        if (input.ProposalType == NetworkDaoOrgType.Parliament)
        {
            if (await IsBp(input.ChainId, input.Address))
            {
                (totalCount, daoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
                {
                    MaxResultCount = input.MaxResultCount,
                    SkipCount = input.SkipCount,
                    Sorting = input.Sorting,
                    ChainId = input.ChainId,
                    OrgType = NetworkDaoOrgType.Parliament,
                    OrgAddress = input.Search
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
            if (!orgMemberIndices.IsNullOrEmpty())
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

    public async Task<GetOrgOfProposerListPagedResult> GetOrgOfProposerListAsync(GetOrgOfProposerListInput input)
    {
        var totalCount = 0L;
        var daoOrgIndices = new List<NetworkDaoOrgIndex>();
        if (input.ProposalType == NetworkDaoOrgType.Parliament)
        {
            if (await IsBp(input.ChainId, input.Address) ||
                await IsParliamentWhiteListProposer(input.ChainId, input.Address))
            {
                (totalCount, daoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
                {
                    MaxResultCount = input.MaxResultCount,
                    SkipCount = input.SkipCount,
                    Sorting = input.Sorting,
                    ChainId = input.ChainId,
                    OrgType = NetworkDaoOrgType.Parliament,
                    OrgAddress = input.Search
                });
            }
        }
        else if (input.ProposalType is NetworkDaoOrgType.Association or NetworkDaoOrgType.Referendum)
        {
            var (proposerTotalCount, proposerIndices) = await _networkDaoEsDataProvider.GetOrgProposerListAsync(
                new GetOrgProposerListInput
                {
                    MaxResultCount = input.MaxResultCount,
                    SkipCount = input.SkipCount,
                    Sorting = input.Sorting,
                    ChainId = input.ChainId,
                    OrgType = input.ProposalType,
                    Address = input.Address,
                    OrgAddress = input.Search
                });
            if (!proposerIndices.IsNullOrEmpty())
            {
                var orgAddressList = proposerIndices.Select(t => t.OrgAddress).Distinct().ToList();
                (totalCount, daoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
                {
                    ChainId = input.ChainId,
                    OrgAddresses = orgAddressList
                });
            }
        }
        else
        {
            throw new UserFriendlyException("Invalid ProposalType!");
        }

        var resultDtos =
            _objectMapper.Map<List<NetworkDaoOrgIndex>, List<GetOrgOfProposerListResultDto>>(daoOrgIndices);

        return new GetOrgOfProposerListPagedResult()
        {
            Items = resultDtos,
            TotalCount = totalCount
        };
    }

    private async Task<bool> IsParliamentWhiteListProposer(string chainId, string address)
    {
        var (totalCount, proposerIndices) = await _networkDaoEsDataProvider.GetOrgProposerListAsync(
            new GetOrgProposerListInput
            {
                ChainId = chainId,
                OrgType = NetworkDaoOrgType.Parliament
            });

        if (!proposerIndices.IsNullOrEmpty() && proposerIndices.Exists(t => t.Proposer == address))
        {
            return true;
        }

        return false;
    }

    private async Task<bool> IsBp(string chainId, string address)
    {
        var bpList = await _graphQlProvider.GetBPAsync(chainId);
        var isBp = bpList.Contains(address);
        return isBp;
    }
    
    private async Task<Dictionary<string, List<string>>> GetOrgMemberDictionaryAsync(GetOrganizationsInput input, List<string> orgAddressList)
    {
        var orgMemberDictionary = new Dictionary<string, List<string>>();
        if (input.ProposalType != NetworkDaoOrgType.Association)
        {
            return orgMemberDictionary;
        }

        var (totalCount, networkDaoOrgMemberIndices) = await _networkDaoEsDataProvider.GetOrgMemberListAsync(
            new GetOrgMemberListInput
            {
                ChainId = input.ChainId,
                OrgAddresses = orgAddressList
            });
        orgMemberDictionary = networkDaoOrgMemberIndices.GroupBy(t => t.OrgAddress)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Member).ToList());
        return orgMemberDictionary;

    }

    private async Task<Dictionary<string, List<string>>> GetOrgProposerWhiteListDictionaryAsync(GetOrganizationsInput input, List<string> orgAddressList)
    {
        var proposerDictionary = new Dictionary<string, List<string>>();
        if (input.ProposalType is not (NetworkDaoOrgType.Association or NetworkDaoOrgType.Referendum))
        {
            return proposerDictionary;
        }

        var (totalCount, orgProposerIndices) = await _networkDaoEsDataProvider.GetOrgProposerListAsync(
            new GetOrgProposerListInput
            {
                ChainId = input.ChainId,
                OrgType = NetworkDaoOrgType.All,
                OrgAddresses = orgAddressList
            });
        if (!orgProposerIndices.IsNullOrEmpty())
        {
            proposerDictionary = orgProposerIndices.GroupBy(t => t.OrgAddress)
                .ToDictionary(g => g.Key, g => g.Select(t => t.Proposer).ToList());
        }
        return proposerDictionary;
    }
}