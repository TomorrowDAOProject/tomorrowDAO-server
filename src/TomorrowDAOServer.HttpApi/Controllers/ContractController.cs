using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Contract.Dto;
using Volo.Abp;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Contract")]
[Route("api/app/contract/")]
public class ContractController
{
    private readonly IContractService _contractService;

    public ContractController(IContractService contractService)
    {
        _contractService = contractService;
    }
    
    [HttpGet("function-list")]
    public FunctionDetailDto FunctionList(QueryFunctionListInput input)
    {
        return _contractService.GetFunctionList(input.ChainId, input.ContractAddress);
    }
    
    [HttpGet("contracts-info")]
    public async Task<ContractDetailDto> ContractsInfoAsync(QueryContractsInfoInput input)
    {
        return await _contractService.GetContractInfoAsync(input);
    }
}