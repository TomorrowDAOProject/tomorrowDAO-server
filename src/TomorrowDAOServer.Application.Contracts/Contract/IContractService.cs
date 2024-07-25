using System.Collections.Generic;
using System.Threading.Tasks;
using TomorrowDAOServer.Contract.Dto;

namespace TomorrowDAOServer.Contract;

public interface IContractService
{
    FunctionDetailDto GetFunctionList(string chainId, string contractAddress);
    Task<ContractDetailDto> GetContractInfoAsync(QueryContractsInfoInput input);
}