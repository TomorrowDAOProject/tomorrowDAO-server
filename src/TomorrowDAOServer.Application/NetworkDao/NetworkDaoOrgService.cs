using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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
    private readonly ILogger<NetworkDaoOrgService> _logger;

    public NetworkDaoOrgService(INetworkDaoEsDataProvider networkDaoEsDataProvider,
        IGraphQLProvider graphQlProvider, IObjectMapper objectMapper, ILogger<NetworkDaoOrgService> logger)
    {
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _graphQlProvider = graphQlProvider;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<GetOrganizationsPagedResult> GetOrganizationsAsync(GetOrganizationsInput input)
    {
        var getOrgListInput = _objectMapper.Map<GetOrganizationsInput, GetOrgListInput>(input);
        var (totalCount, networkDaoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(getOrgListInput);
        var resultDtos = _objectMapper.Map<List<NetworkDaoOrgIndex>, List<NetworkDaoOrgDto>>(networkDaoOrgIndices);
        var bpList = await _graphQlProvider.GetBPAsync(input.ChainId);
        if (!resultDtos.IsNullOrEmpty())
        {
            var orgAddressList = networkDaoOrgIndices.Select(t => t.OrgAddress).ToList();
            var orgProposerWhiteListDictionaryTask = GetOrgProposerWhiteListDictionaryAsync(input.ChainId, input.ProposalType, orgAddressList);
            var orgMemberDictionaryTask = GetOrgMemberDictionaryAsync(input.ChainId, input.ProposalType, orgAddressList);

            var orgProposerDictionary = await orgProposerWhiteListDictionaryTask;
            var orgMemberDictionary = await orgMemberDictionaryTask;

            foreach (var resultDto in resultDtos)
            {
                resultDto.NetworkDaoOrgLeftOrgInfoDto ??= new NetworkDaoOrgLeftOrgInfoDto();
                if (orgProposerDictionary.ContainsKey(resultDto.OrgAddress))
                {
                    resultDto.NetworkDaoOrgLeftOrgInfoDto.ProposerWhiteList ??= new ProposerWhiteList();
                    resultDto.NetworkDaoOrgLeftOrgInfoDto.ProposerWhiteList.Proposers = orgProposerDictionary[resultDto.OrgAddress];
                }
                if (orgMemberDictionary.ContainsKey(resultDto.OrgAddress))
                {
                    resultDto.NetworkDaoOrgLeftOrgInfoDto.OrganizationMemberList ??= new OrganizationMemberList();
                    resultDto.NetworkDaoOrgLeftOrgInfoDto.OrganizationMemberList.OrganizationMembers =
                        orgMemberDictionary[resultDto.OrgAddress];
                }
            }
        }

        return new GetOrganizationsPagedResult
        {
            Items = resultDtos,
            TotalCount = totalCount,
            BpList = bpList
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
            var isBp = await IsBp(input.ChainId, input.Address);
            var isParliamentWhiteListProposer = await IsParliamentWhiteListProposer(input.ChainId, input.Address);
            if (isBp || isParliamentWhiteListProposer)
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
            else
            {
                (totalCount, daoOrgIndices) = await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
                {
                    MaxResultCount = input.MaxResultCount,
                    SkipCount = input.SkipCount,
                    Sorting = input.Sorting,
                    ChainId = input.ChainId,
                    OrgType = NetworkDaoOrgType.Parliament,
                    OrgAddress = input.Search,
                    ProposerAuthorityRequired = false
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

    public async Task<bool> IsBp(string chainId, string address)
    {
        var bpList = await _graphQlProvider.GetBPAsync(chainId);
        var isBp = bpList.Contains(address);
        return isBp;
    }

    public NetworkDaoOrgDto ConvertToOrgDto(NetworkDaoOrgIndex orgIndex, List<string> orgMemberList, List<string> orgProposerList)
    {
        var networkDaoOrgDto = _objectMapper.Map<NetworkDaoOrgIndex, NetworkDaoOrgDto>(orgIndex);
        networkDaoOrgDto.NetworkDaoOrgLeftOrgInfoDto ??= new NetworkDaoOrgLeftOrgInfoDto();
        if (!orgProposerList.IsNullOrEmpty())
        {
            networkDaoOrgDto.NetworkDaoOrgLeftOrgInfoDto.ProposerWhiteList ??= new ProposerWhiteList();
            networkDaoOrgDto.NetworkDaoOrgLeftOrgInfoDto.ProposerWhiteList.Proposers = new List<string>(orgProposerList);
        }
        if (!orgMemberList.IsNullOrEmpty())
        {
            networkDaoOrgDto.NetworkDaoOrgLeftOrgInfoDto.OrganizationMemberList ??= new OrganizationMemberList();
            networkDaoOrgDto.NetworkDaoOrgLeftOrgInfoDto.OrganizationMemberList.OrganizationMembers =
                new List<string>(orgMemberList);
        }

        return networkDaoOrgDto;
    }

    public async Task<Dictionary<string, List<string>>> GetOrgMemberDictionaryAsync(string chainId, NetworkDaoOrgType orgType, List<string> orgAddressList)
    {
        var orgMemberDictionary = new Dictionary<string, List<string>>();
        if (chainId.IsNullOrWhiteSpace() || orgAddressList.IsNullOrEmpty())
        {
            return orgMemberDictionary;
        }
        
        if (orgType != NetworkDaoOrgType.Association)
        {
            return orgMemberDictionary;
        }

        var (totalCount, networkDaoOrgMemberIndices) = await _networkDaoEsDataProvider.GetOrgMemberListAsync(
            new GetOrgMemberListInput
            {
                ChainId = chainId,
                OrgAddresses = orgAddressList
            });
        orgMemberDictionary = networkDaoOrgMemberIndices.GroupBy(t => t.OrgAddress)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Member).ToList());
        return orgMemberDictionary;

    }

    public async Task<Dictionary<string, NetworkDaoOrgIndex>> GetOrgDictionaryAsync(string chainId, List<string> orgAddresses)
    {
        if (chainId.IsNullOrWhiteSpace() || orgAddresses.IsNullOrEmpty())
        {
            return new Dictionary<string, NetworkDaoOrgIndex>();
        }
        var (totalCount, orgIndices) =  await _networkDaoEsDataProvider.GetOrgIndexAsync(new GetOrgListInput
        {
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
            SkipCount = 0,
            ChainId = chainId,
            OrgAddresses = orgAddresses
        });
        orgIndices ??= new List<NetworkDaoOrgIndex>();
        return orgIndices.ToDictionary(t => t.OrgAddress, t => t);
    }

    public async Task<Dictionary<string, List<string>>> GetOrgProposerWhiteListDictionaryAsync(string chainId, NetworkDaoOrgType orgType, List<string> orgAddressList)
    {
        var proposerDictionary = new Dictionary<string, List<string>>();
        if (chainId.IsNullOrWhiteSpace() || orgAddressList.IsNullOrEmpty())
        {
            return proposerDictionary;
        }
        if (orgType is not (NetworkDaoOrgType.Association or NetworkDaoOrgType.Referendum))
        {
            return proposerDictionary;
        }

        var (totalCount, orgProposerIndices) = await _networkDaoEsDataProvider.GetOrgProposerListAsync(
            new GetOrgProposerListInput
            {
                ChainId = chainId,
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