using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.NetworkDao.Dto;

public class NetworkDaoPagedResultDto<T> :  PagedResultDto<T>
{
}

public class NetworkDaoProposalDto
{
    public string ProposalId { get; set; }
    public string OrganizationAddress { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int ProposalType { get; set; }

    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public bool IsDeleted { get; set; }
}

public class GetNetworkDaoProposalsInput
{
    public string ChainId { get; set; }
    public List<string> ProposalIds { get; set; }
    public NetworkDaoProposalType ProposalType { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 10;
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
}

public enum NetworkDaoProposalType
{
    All = 0,
    Parliament = 1,
    Association = 2,
    Referendum = 3
}