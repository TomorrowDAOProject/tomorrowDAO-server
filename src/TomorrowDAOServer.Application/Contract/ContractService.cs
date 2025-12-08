using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Contract.Dto;
using TomorrowDAOServer.Contract.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.Contract;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ContractService : TomorrowDAOServerAppService, IContractService
{
    private readonly ILogger<ContractService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IContractProvider _contractProvider;
    private readonly IDAOProvider _daoProvider;

    public ContractService(IObjectMapper objectMapper, IContractProvider contractProvider, IDAOProvider daoProvider,
        ILogger<ContractService> logger)
    {
        _objectMapper = objectMapper;
        _contractProvider = contractProvider;
        _daoProvider = daoProvider;
        _logger = logger;
    }

    public FunctionDetailDto GetFunctionList(string chainId, string contractAddress)
    {
        var contractInfo = _contractProvider.GetContractInfo(chainId, contractAddress).FirstOrDefault();
        return new FunctionDetailDto
        {
            FunctionList = contractInfo?.FunctionList ?? new List<string>()
        };
    }

    public async Task<ContractDetailDto> GetContractInfoAsync(QueryContractsInfoInput input)
    {
        var contractInfos = _contractProvider.GetContractInfo(input.ChainId, string.Empty);

        var governanceMechanism = input.GovernanceMechanism;
        if (governanceMechanism == null && !input.DaoId.IsNullOrWhiteSpace())
        {
            var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
            {
                ChainId = input.ChainId,
                DAOId = input.DaoId
            });
            if (daoIndex != null)
            {
                governanceMechanism = daoIndex.GovernanceMechanism;
            }
        }

        var contractInfoList = _objectMapper.Map<List<ContractInfo>, List<ContractInfoDto>>(contractInfos);
        foreach (var contractInfoDto in contractInfoList)
        {
            if (contractInfoDto.FunctionList.IsNullOrEmpty())
            {
                continue;
            }

            if (contractInfoDto.MultiSigDaoMethodBlacklist.IsNullOrEmpty())
            {
                continue;
            }

            //MultiSig Dao excluded method list
            if (governanceMechanism != null && governanceMechanism == GovernanceMechanism.Organization)
            {
                foreach (var methodName in contractInfoDto.MultiSigDaoMethodBlacklist)
                {
                    contractInfoDto.FunctionList.Remove(methodName);
                }
            }
        }

        contractInfoList = contractInfoList.Where(item => !item.FunctionList.IsNullOrEmpty()).ToList();
        
        return new ContractDetailDto
        {
            ContractInfoList = contractInfoList
        };
    }
}