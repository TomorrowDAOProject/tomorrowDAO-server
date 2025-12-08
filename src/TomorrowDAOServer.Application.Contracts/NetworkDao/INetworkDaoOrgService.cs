using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Dtos;

namespace TomorrowDAOServer.NetworkDao;

public interface INetworkDaoOrgService
{
    Task<GetOrganizationsPagedResult> GetOrganizationsAsync(GetOrganizationsInput input);
    Task<GetOrgOfOwnerListPagedResult> GetOrgOfOwnerListAsync(GetOrgOfOwnerListInput input);
    Task<GetOrgOfProposerListPagedResult> GetOrgOfProposerListAsync(GetOrgOfProposerListInput input);

    Task<Dictionary<string, List<string>>> GetOrgProposerWhiteListDictionaryAsync(string chainId,
        NetworkDaoOrgType orgType, List<string> orgAddressList);
    Task<Dictionary<string, List<string>>> GetOrgMemberDictionaryAsync(string chainId, NetworkDaoOrgType orgType,
        List<string> orgAddressList);
    Task<Dictionary<string,NetworkDaoOrgIndex>> GetOrgDictionaryAsync(string chainId, List<string> orgAddresses);
    Task<bool> IsBp(string chainId, string address);
    Task<NetworkDaoOrgDto> ConvertToOrgDtoAsync(NetworkDaoOrgIndex orgIndex, List<string> orgMemberList,
        List<string> orgProposerList);
}