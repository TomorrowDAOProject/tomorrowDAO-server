using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Index;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Migrator.GraphQL;
using TomorrowDAOServer.NetworkDao.Provider;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.NetworkDao.Sync;

public interface INetworkDaoOrgSyncService
{
    Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight);
}

public class NetworkDaoOrgSyncService : INetworkDaoOrgSyncService, ISingletonDependency
{
    private readonly ILogger<NetworkDaoOrgSyncService> _logger;
    private readonly INetworkDaoGraphQlDataProvider _networkDaoGraphQlDataProvider;
    private readonly INetworkDaoContractProvider _networkDaoContractProvider;
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly IObjectMapper _objectMapper;

    private static readonly int MaxResultCount = 20; //LimitedResultRequestDto.MaxMaxResultCount;

    public NetworkDaoOrgSyncService(ILogger<NetworkDaoOrgSyncService> logger,
        INetworkDaoGraphQlDataProvider networkDaoGraphQlDataProvider,
        INetworkDaoContractProvider networkDaoContractProvider, IObjectMapper objectMapper,
        INetworkDaoEsDataProvider networkDaoEsDataProvider)
    {
        _logger = logger;
        _networkDaoGraphQlDataProvider = networkDaoGraphQlDataProvider;
        _networkDaoContractProvider = networkDaoContractProvider;
        _objectMapper = objectMapper;
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
    }

    public async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        List<IndexerOrgChanged> queryList;
        do
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[NetworkDaoOrgMigrator]Sync Org, BlockHeight:{0}-{1}, SkipCount={2}",
                lastEndHeight, newIndexHeight, skipCount);
            //query the proposal data of changed
            queryList 
                = (await _networkDaoGraphQlDataProvider.GetNetworkDaoOrgChangedIndexAsync(
                new GetOrgChangedIndexInput
                {
                    ChainId = chainId,
                    OrgType = NetworkDaoOrgType.All,
                    StartBlockHeight = lastEndHeight,
                    EndBlockHeight = newIndexHeight,
                    SkipCount = skipCount,
                    MaxResultCount = MaxResultCount
                })).Data;
            _logger.LogInformation("[NetworkDaoOrgMigrator]Sync Org,count:{count}", queryList?.Count);
            if (queryList.IsNullOrEmpty())
            {
                break;
            }

            var orgIndices = new List<NetworkDaoOrgIndex>();
            var orgMemberIndices = new List<NetworkDaoOrgMemberIndex>();
            var orgProposerIndices = new List<NetworkDaoOrgProposerIndex>();
            foreach (var orgChanged in queryList)
            {
                Tuple<NetworkDaoOrgIndex, List<NetworkDaoOrgMemberIndex>, List<NetworkDaoOrgProposerIndex>> tuple;
                switch (orgChanged.OrgType)
                {
                    case NetworkDaoOrgType.Parliament:
                        tuple = await BuildParliamentIndexAsync(chainId, orgChanged);
                        break;
                    case NetworkDaoOrgType.Association:
                        tuple = await BuildAssociationIndexAsync(chainId, orgChanged);
                        break;
                    case NetworkDaoOrgType.Referendum:
                        tuple = await BuildReferendumIndexAsync(chainId, orgChanged);
                        break;
                    default:
                        continue;
                }

                var (orgIndex, orgMemberList, orgProposerList) = tuple;
                await UpdateOrgIndexAsync(chainId, orgChanged, orgIndex);
                orgIndices.Add(orgIndex);
                orgMemberIndices.AddRange(orgMemberList);
                orgProposerIndices.AddRange(orgProposerList);
            }

            //Delete Members
            await DeleteOrgMemberIndexAsync(chainId, queryList);
            //Delete Proposers
            await DeleteOrgProposerIndexAsync(chainId, queryList);

            await _networkDaoEsDataProvider.BulkAddOrUpdateOrgIndexAsync(orgIndices);
            await _networkDaoEsDataProvider.BulkAddOrUpdateOrgMemberIndexAsync(orgMemberIndices);
            await _networkDaoEsDataProvider.BulkAddOrUpdateOrgProposerIndexAsync(orgProposerIndices);

            skipCount += queryList.Count;
            stopwatch.Stop();
            _logger.LogInformation("[NetworkDaoOrgMigrator]0.Sync Org, count={0}, duration={1}", queryList.Count,
                stopwatch.ElapsedMilliseconds);
        } while (queryList.Count == MaxResultCount);

        return newIndexHeight;
    }

    private async Task DeleteOrgMemberIndexAsync(string chainId, List<IndexerOrgChanged> queryList)
    {
        var stopwatch = Stopwatch.StartNew();
        var orgAddressList = queryList.Select(t => t.OrganizationAddress).ToList();
        var orgMemberList = await _networkDaoEsDataProvider.GetOrgMemberListByOrgAddressAsync(chainId, orgAddressList);
        await _networkDaoEsDataProvider.BulkDeleteOrgMemberIndexAsync(orgMemberList);
        stopwatch.Stop();
        _logger.LogInformation("[NetworkDaoOrgMigrator]delete org member, count={0}, duration={1}", orgMemberList.Count,
            stopwatch.ElapsedMilliseconds);
    }
    
    private async Task DeleteOrgProposerIndexAsync(string chainId, List<IndexerOrgChanged> queryList)
    {
        var stopwatch = Stopwatch.StartNew();
        var orgAddressList = queryList.Select(t => t.OrganizationAddress).ToList();
        var orgProposerList = await _networkDaoEsDataProvider.GetOrgProposerListByOrgAddressAsync(
            new GetOrgProposerByOrgAddressInput
            {
                ChainId = chainId,
                OrgAddressList = orgAddressList
            });
        await _networkDaoEsDataProvider.BulkDeleteOrgProposerIndexAsync(orgProposerList.Item2);
        stopwatch.Stop();
        _logger.LogInformation("[NetworkDaoOrgMigrator]delete org proposer, count={0}, duration={1}",
            orgProposerList.Item2.Count, stopwatch.ElapsedMilliseconds);
    }

    private async Task UpdateOrgIndexAsync(string chainId, IndexerOrgChanged orgChanged, NetworkDaoOrgIndex orgIndex)
    {
        //Creator
        if (orgChanged.TransactionInfo.IsAAForwardCall)
        {
            var caHash = orgChanged.TransactionInfo.CAHash;
            var caContractAddress = orgChanged.TransactionInfo.PortKeyContract;

            var getHolderInfoOutput =
                await _networkDaoContractProvider.GetHolderInfoAsync(chainId, caContractAddress, caHash);
            orgIndex.Creator = getHolderInfoOutput.CaAddress?.ToBase58() ?? string.Empty;
        }
        else
        {
            orgIndex.Creator = orgChanged.TransactionInfo.From ?? string.Empty;
        }

        orgIndex.CreatedAt = DateTime.Now;
        orgIndex.UpdatedAt = DateTime.Now;
    }

    private async Task<Tuple<NetworkDaoOrgIndex, List<NetworkDaoOrgMemberIndex>, List<NetworkDaoOrgProposerIndex>>>
        BuildParliamentIndexAsync(string chainId, IndexerOrgChanged orgChanged)
    {
        AElf.Contracts.Parliament.Organization organization =
            await _networkDaoContractProvider.GetParliamentOrganizationAsync(chainId, orgChanged.OrganizationAddress);

        var orgIndex = _objectMapper.Map<IndexerOrgChanged, NetworkDaoOrgIndex>(orgChanged);
        orgIndex.Id =
            IdGeneratorHelper.GenerateId(chainId, orgChanged.OrgType.ToString(), orgChanged.OrganizationAddress);
        orgIndex.OrgHash = organization.OrganizationHash?.ToHex() ?? string.Empty;
        orgIndex.MinimalApprovalThreshold = organization.ProposalReleaseThreshold?.MinimalApprovalThreshold ?? 0;
        orgIndex.MaximalRejectionThreshold = organization.ProposalReleaseThreshold?.MaximalRejectionThreshold ?? 0;
        orgIndex.MaximalAbstentionThreshold = organization.ProposalReleaseThreshold?.MaximalAbstentionThreshold ?? 0;
        orgIndex.MinimalVoteThreshold = organization.ProposalReleaseThreshold?.MinimalVoteThreshold ?? 0;
        orgIndex.ParliamentMemberProposingAllowed = organization.ParliamentMemberProposingAllowed;
        orgIndex.CreationToken = organization.CreationToken?.ToHex() ?? string.Empty;
        orgIndex.ProposerAuthorityRequired = organization.ProposerAuthorityRequired;

        //Only DefaultOrganizationAddress can change the whitelist. The Parliament whitelist can be queried by OrgType
        var proposerWhiteList = await _networkDaoContractProvider.GetParliamentOrgProposerWhiteListAsync(chainId);
        var orgProposerIndices = new List<NetworkDaoOrgProposerIndex>();
        if (!proposerWhiteList.IsNullOrEmpty())
        {
            orgProposerIndices.AddRange(proposerWhiteList.Select(proposer =>
                new NetworkDaoOrgProposerIndex
                {
                    Id = IdGeneratorHelper.GenerateId(chainId, orgChanged.OrganizationAddress, proposer),
                    ChainId = chainId,
                    OrgAddress = orgChanged.OrganizationAddress,
                    RelatedTxId = orgChanged.TransactionInfo.TransactionId ?? string.Empty,
                    Proposer = proposer,
                    OrgType = orgChanged.OrgType
                }));
        }

        return new Tuple<NetworkDaoOrgIndex, List<NetworkDaoOrgMemberIndex>, List<NetworkDaoOrgProposerIndex>>(orgIndex,
            new List<NetworkDaoOrgMemberIndex>(), orgProposerIndices);
    }

    private async Task<Tuple<NetworkDaoOrgIndex, List<NetworkDaoOrgMemberIndex>, List<NetworkDaoOrgProposerIndex>>>
        BuildAssociationIndexAsync(string chainId, IndexerOrgChanged orgChanged)
    {
        AElf.Contracts.Association.Organization organization =
            await _networkDaoContractProvider.GetAssociationOrganizationAsync(chainId, orgChanged.OrganizationAddress);

        var orgIndex = _objectMapper.Map<IndexerOrgChanged, NetworkDaoOrgIndex>(orgChanged);
        orgIndex.Id =
            IdGeneratorHelper.GenerateId(chainId, orgChanged.OrgType.ToString(), orgChanged.OrganizationAddress);
        orgIndex.OrgHash = organization.OrganizationHash?.ToHex() ?? string.Empty;
        orgIndex.MinimalApprovalThreshold = organization.ProposalReleaseThreshold?.MinimalApprovalThreshold ?? 0;
        orgIndex.MaximalRejectionThreshold = organization.ProposalReleaseThreshold?.MaximalRejectionThreshold ?? 0;
        orgIndex.MaximalAbstentionThreshold = organization.ProposalReleaseThreshold?.MaximalAbstentionThreshold ?? 0;
        orgIndex.MinimalVoteThreshold = organization.ProposalReleaseThreshold?.MinimalVoteThreshold ?? 0;
        orgIndex.CreationToken = organization.CreationToken?.ToHex() ?? string.Empty;

        var orgMemberIndices = new List<NetworkDaoOrgMemberIndex>();
        if (organization.OrganizationMemberList != null &&
            !organization.OrganizationMemberList.OrganizationMembers.IsNullOrEmpty())
        {
            orgMemberIndices.AddRange(organization.OrganizationMemberList.OrganizationMembers.Select(
                organizationMember => new NetworkDaoOrgMemberIndex
                {
                    Id = IdGeneratorHelper.GenerateId(chainId,
                        orgChanged.OrganizationAddress,
                        organizationMember.ToBase58()),
                    ChainId = chainId,
                    OrgAddress = orgChanged.OrganizationAddress,
                    Member = organizationMember.ToBase58(),
                    CreatedAt = DateTime.Now
                }));
        }

        var orgProposerIndices = new List<NetworkDaoOrgProposerIndex>();
        if (organization.ProposerWhiteList != null && !organization.ProposerWhiteList.Proposers.IsNullOrEmpty())
        {
            orgProposerIndices.AddRange(organization.ProposerWhiteList.Proposers.Select(proposer =>
                new NetworkDaoOrgProposerIndex
                {
                    Id = IdGeneratorHelper.GenerateId(chainId,
                        orgChanged.OrganizationAddress,
                        proposer.ToBase58()),
                    ChainId = chainId,
                    OrgAddress = orgChanged.OrganizationAddress,
                    RelatedTxId = orgChanged.TransactionInfo.TransactionId ?? string.Empty,
                    Proposer = proposer.ToBase58(),
                    OrgType = orgChanged.OrgType
                }));
        }

        return new Tuple<NetworkDaoOrgIndex, List<NetworkDaoOrgMemberIndex>, List<NetworkDaoOrgProposerIndex>>(orgIndex,
            orgMemberIndices, orgProposerIndices);
    }

    private async Task<Tuple<NetworkDaoOrgIndex, List<NetworkDaoOrgMemberIndex>, List<NetworkDaoOrgProposerIndex>>>
        BuildReferendumIndexAsync(string chainId, IndexerOrgChanged orgChanged)
    {
        AElf.Contracts.Referendum.Organization organization =
            await _networkDaoContractProvider.GetReferendumOrganizationAsync(chainId, orgChanged.OrganizationAddress);

        var orgIndex = _objectMapper.Map<IndexerOrgChanged, NetworkDaoOrgIndex>(orgChanged);
        orgIndex.Id =
            IdGeneratorHelper.GenerateId(chainId, orgChanged.OrgType.ToString(), orgChanged.OrganizationAddress);
        orgIndex.OrgHash = organization.OrganizationHash?.ToHex() ?? string.Empty;
        orgIndex.MinimalApprovalThreshold = organization.ProposalReleaseThreshold?.MinimalApprovalThreshold ?? 0;
        orgIndex.MaximalRejectionThreshold = organization.ProposalReleaseThreshold?.MaximalRejectionThreshold ?? 0;
        orgIndex.MaximalAbstentionThreshold = organization.ProposalReleaseThreshold?.MaximalAbstentionThreshold ?? 0;
        orgIndex.MinimalVoteThreshold = organization.ProposalReleaseThreshold?.MinimalVoteThreshold ?? 0;
        orgIndex.CreationToken = organization.CreationToken?.ToHex() ?? string.Empty;
        orgIndex.TokenSymbol = organization.TokenSymbol;

        var orgMemberIndices = new List<NetworkDaoOrgMemberIndex>();
        var orgProposerIndices = new List<NetworkDaoOrgProposerIndex>();
        if (organization.ProposerWhiteList != null && !organization.ProposerWhiteList.Proposers.IsNullOrEmpty())
        {
            orgProposerIndices.AddRange(organization.ProposerWhiteList.Proposers.Select(proposer =>
                new NetworkDaoOrgProposerIndex
                {
                    Id = IdGeneratorHelper.GenerateId(chainId,
                        orgChanged.OrganizationAddress,
                        proposer.ToBase58()),
                    ChainId = chainId,
                    OrgAddress = orgChanged.OrganizationAddress,
                    RelatedTxId = orgChanged.TransactionInfo.TransactionId ?? string.Empty,
                    Proposer = proposer.ToBase58(),
                    OrgType = orgChanged.OrgType
                }));
        }

        return new Tuple<NetworkDaoOrgIndex, List<NetworkDaoOrgMemberIndex>, List<NetworkDaoOrgProposerIndex>>(orgIndex,
            orgMemberIndices, orgProposerIndices);
    }
}