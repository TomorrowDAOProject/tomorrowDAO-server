using System;
using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Portkey.Contracts.CA;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Enums;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.NetworkDao.Provider;

public interface INetworkDaoContractProvider
{
    Task<ProposalOutput> GetProposalAsync(string chainId, NetworkDaoOrgType orgType, string proposalId);
    Task<GetHolderInfoOutput> GetHolderInfoAsync(string chainId, string caContractAddress, string caHash);

    Task<AElf.Contracts.Parliament.Organization> GetParliamentOrganizationAsync(string chainId,
        string organizationAddress);

    Task<AElf.Contracts.Association.Organization> GetAssociationOrganizationAsync(string chainId,
        string organizationAddress);

    Task<AElf.Contracts.Referendum.Organization> GetReferendumOrganizationAsync(string chainId,
        string organizationAddress);
}

public class NetworkDaoContractProvider : INetworkDaoContractProvider, ISingletonDependency
{
    private readonly ILogger<NetworkDaoContractProvider> _logger;
    private readonly IContractProvider _contractProvider;

    public NetworkDaoContractProvider(ILogger<NetworkDaoContractProvider> logger, IContractProvider contractProvider)
    {
        _logger = logger;
        _contractProvider = contractProvider;
    }

    public async Task<ProposalOutput> GetProposalAsync(string chainId, NetworkDaoOrgType orgType, string proposalId)
    {
        var contractName = GetOrgContractName(chainId, orgType);
        var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
            contractName, CommonConstant.Acs3MethodGetProposal, Hash.LoadFromHex(proposalId));
        return await _contractProvider.CallTransactionAsync<ProposalOutput>(chainId, transaction);
    }

    public async Task<GetHolderInfoOutput> GetHolderInfoAsync(string chainId, string caContractAddress, string caHash)
    {
        if (caContractAddress.IsNullOrWhiteSpace() || caHash.IsNullOrWhiteSpace())
        {
            return new GetHolderInfoOutput();
        }

        var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
            CommonConstant.CaContractAddressName, CommonConstant.CaMethodGetHolderInfo, new GetHolderInfoInput
            {
                CaHash = Hash.LoadFromHex(caHash),
                LoginGuardianIdentifierHash = Hash.Empty
            },
            caContractAddress);
        return await _contractProvider.CallTransactionWithMessageAsync<GetHolderInfoOutput>(chainId, transaction);
    }

    public async Task<AElf.Contracts.Parliament.Organization> GetParliamentOrganizationAsync(string chainId,
        string organizationAddress)
    {
        if (chainId.IsNullOrWhiteSpace() || organizationAddress.IsNullOrWhiteSpace())
        {
            return new AElf.Contracts.Parliament.Organization();
        }

        var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
            SystemContractName.ParliamentContract, CommonConstant.OrganizationMethodGetOrganization,
            Address.FromBase58(organizationAddress));
        return await _contractProvider.CallTransactionAsync<AElf.Contracts.Parliament.Organization>(chainId,
            transaction);
    }

    public async Task<AElf.Contracts.Association.Organization> GetAssociationOrganizationAsync(string chainId,
        string organizationAddress)
    {
        if (chainId.IsNullOrWhiteSpace() || organizationAddress.IsNullOrWhiteSpace())
        {
            return new AElf.Contracts.Association.Organization();
        }

        var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
            SystemContractName.AssociationContract, CommonConstant.OrganizationMethodGetOrganization,
            Address.FromBase58(organizationAddress));
        return await _contractProvider.CallTransactionAsync<AElf.Contracts.Association.Organization>(chainId,
            transaction);
    }

    public async Task<AElf.Contracts.Referendum.Organization> GetReferendumOrganizationAsync(string chainId,
        string organizationAddress)
    {
        if (chainId.IsNullOrWhiteSpace() || organizationAddress.IsNullOrWhiteSpace())
        {
            return new AElf.Contracts.Referendum.Organization();
        }

        var (_, transaction) = await _contractProvider.CreateCallTransactionAsync(chainId,
            SystemContractName.ReferendumContract, CommonConstant.OrganizationMethodGetOrganization,
            Address.FromBase58(organizationAddress));
        return await _contractProvider.CallTransactionAsync<AElf.Contracts.Referendum.Organization>(chainId,
            transaction);
    }

    private string GetOrgContractName(string chainId, NetworkDaoOrgType orgType)
    {
        return orgType switch
        {
            NetworkDaoOrgType.Parliament => SystemContractName.ParliamentContract,
            NetworkDaoOrgType.Association => SystemContractName.AssociationContract,
            NetworkDaoOrgType.Referendum => SystemContractName.ReferendumContract,
            _ => string.Empty
        };
    }
}