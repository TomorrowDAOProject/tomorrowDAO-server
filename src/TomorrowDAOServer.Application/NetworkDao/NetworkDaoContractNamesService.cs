using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS3;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.NetworkDao.Dtos;
using TomorrowDAOServer.NetworkDao.Migrator.ES;
using TomorrowDAOServer.NetworkDao.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace TomorrowDAOServer.NetworkDao;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class NetworkDaoContractNamesService : TomorrowDAOServerAppService, INetworkDaoContractNamesService
{
    private readonly ILogger<NetworkDaoContractNamesService> _logger;
    private readonly INetworkDaoEsDataProvider _networkDaoEsDataProvider;
    private readonly INetworkDaoContractProvider _networkDaoContractProvider;
    private readonly IUserProvider _userProvider;
    private readonly IExplorerProvider _explorerProvider;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;

    public NetworkDaoContractNamesService(ILogger<NetworkDaoContractNamesService> logger,
        INetworkDaoEsDataProvider networkDaoEsDataProvider, INetworkDaoContractProvider networkDaoContractProvider,
        IUserProvider userProvider, IExplorerProvider explorerProvider,
        IOptionsMonitor<TelegramOptions> telegramOptions)
    {
        _logger = logger;
        _networkDaoEsDataProvider = networkDaoEsDataProvider;
        _networkDaoContractProvider = networkDaoContractProvider;
        _userProvider = userProvider;
        _explorerProvider = explorerProvider;
        _telegramOptions = telegramOptions;
    }

    public async Task<AddContractNameResponse> AddContractNamesAsync(AddContractNameInput input)
    {
        if (input.ContractName.IsNullOrWhiteSpace())
        {
            return new AddContractNameResponse
            {
                Success = false,
                Message = "contract name cannot be empty."
            };
        }
        
        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (address.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Access denied");
        }

        var contractNamesIndices = await _networkDaoEsDataProvider.GetContractNamesAsync(new GetContractNamesInput
        {
            ChainId = input.ChainId,
            ContractName = input.ContractName
        });

        if (!contractNamesIndices.IsNullOrEmpty())
        {
            return new AddContractNameResponse
            {
                Success = false,
                Message = "contract name has been taken"
            };
        }

        if (input.ProposalId.IsNullOrWhiteSpace())
        {
            var transactionResult =
                await _networkDaoContractProvider.QueryTransactionResultAsync(input.ChainId, input.TxId);
            if (transactionResult.Status == CommonConstant.TransactionStateMined && transactionResult.Logs
                    .Select(l => l.Name).Contains(CommonConstant.ProposalCreated))
            {
                var voteEventLog = transactionResult.Logs.First(l => l.Name == CommonConstant.ProposalCreated);
                var proposalCreated = LogEventDeserializationHelper.DeserializeLogEvent<ProposalCreated>(voteEventLog);
                var contractNamesIndex = new NetworkDaoContractNamesIndex
                {
                    Id = IdGeneratorHelper.GenerateId(input.ChainId, Guid.NewGuid().ToString()),
                    ChainId = input.ChainId,
                    ContractName = input.ContractName,
                    Address = input.Address,
                    ProposalId = input.ProposalId,
                    TxId = input.TxId,
                    Action = input.Action,
                    CreateAt = input.CreateAt == default ? DateTime.UtcNow : input.CreateAt
                };
                await _networkDaoEsDataProvider.AddOrUpdateContractNameAsync(contractNamesIndex);
                return new AddContractNameResponse
                {
                    Success = true
                };
            }
        }
        else
        {
            var contractNamesIndex = new NetworkDaoContractNamesIndex
            {
                Id = IdGeneratorHelper.GenerateId(input.ChainId, Guid.NewGuid().ToString()),
                ChainId = input.ChainId,
                ContractName = input.ContractName,
                Address = input.Address,
                ProposalId = input.ProposalId,
                TxId = input.TxId,
                Action = input.Action,
                CreateAt = input.CreateAt == default ? DateTime.UtcNow : input.CreateAt
            };
            await _networkDaoEsDataProvider.AddOrUpdateContractNameAsync(contractNamesIndex);
            return new AddContractNameResponse
            {
                Success = true
            };
        }

        return new AddContractNameResponse();
    }

    public async Task<UpdateContractNameResponse> UpdateContractNamesAsync(UpdateContractNameInput input)
    {
        if (input.ContractName.IsNullOrWhiteSpace())
        {
            return new UpdateContractNameResponse
            {
                Success = false,
                Message = "contract name cannot be empty."
            };
        }
        
        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (address.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Access denied");
        }

        var contractNamesIndices = await _networkDaoEsDataProvider.GetContractNamesAsync(new GetContractNamesInput
        {
            ChainId = input.ChainId,
            ContractName = input.ContractName
        });

        if (!contractNamesIndices.IsNullOrEmpty())
        {
            return new UpdateContractNameResponse
            {
                Success = false,
                Message = "contract name has been taken"
            };
        }
        
        contractNamesIndices = await _networkDaoEsDataProvider.GetContractNamesAsync(new GetContractNamesInput
        {
            ChainId = input.ChainId,
            ContractAddress = input.ContractAddress
        });
        if (!contractNamesIndices.IsNullOrEmpty())
        {
            var contractNamesIndex = contractNamesIndices.First();

            if (contractNamesIndex.Address != input.Address)
            {
                return new UpdateContractNameResponse
                {
                    Success = false,
                    Message = "Contract name update failed. You do not have permission to change the name of this contract！"
                };
            }
            
            contractNamesIndex.ContractName = input.ContractName;
            await _networkDaoEsDataProvider.AddOrUpdateContractNameAsync(contractNamesIndex);
        }
        else
        {
            await _networkDaoEsDataProvider.AddOrUpdateContractNameAsync(new NetworkDaoContractNamesIndex
            {
                Id = IdGeneratorHelper.GenerateId(input.ChainId, Guid.NewGuid().ToString()),
                ChainId = input.ChainId,
                ContractName = input.ContractName,
                ContractAddress = input.ContractAddress,
                Address = input.Address,
                Action = NetworkDaoContractNameActionEnum.UPDATE,
                CreateAt = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow
            });
        }

        return new UpdateContractNameResponse
        {
            Success = true
        };
    }

    public async Task<int> LoadContractHistoryDataAsync(LoadContractHistoryInput input)
    {
        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (address.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Access denied");
        }

        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        var explorerContractListResponse = await _explorerProvider.GetContractListAsync(input.ChainId, new ExplorerContractListRequest
        {
            PageSize = input.PageSize,
            PageNum = input.PageNum
        });

        var contractNamesIndices = new List<NetworkDaoContractNamesIndex>();
        foreach (var contractDto in explorerContractListResponse.List)
        {
            contractNamesIndices.Add(new NetworkDaoContractNamesIndex
            {
                Id = IdGeneratorHelper.GenerateId(input.ChainId, Guid.NewGuid().ToString()),
                ChainId = input.ChainId,
                ContractName = contractDto.ContractName == "-1" ? string.Empty : contractDto.ContractName,
                ContractAddress = contractDto.Address,
                Address = contractDto.Author,
                ProposalId = null,
                TxId = null,
                Action = NetworkDaoContractNameActionEnum.ALL,
                CreateAt = DateTime.UtcNow,
                Category = contractDto.Category,
                IsSystemContract = contractDto.IsSystemContract,
                Serial = contractDto.Serial,
                Version = contractDto.Version,
                UpdateTime = contractDto.UpdateTime
            });
        }
        await _networkDaoEsDataProvider.BulkAddOrUpdateContractNameAsync(contractNamesIndices);

        return explorerContractListResponse.Total;
    }

    public async Task<CheckContractNameResponse> CheckContractNameAsync(CheckContractNameInput input)
    {
        if (input.ContractName.IsNullOrWhiteSpace())
        {
            return new CheckContractNameResponse
            {
                IsExist = false
            };
        }
        var contractNamesIndices = await _networkDaoEsDataProvider.GetContractNamesAsync(new GetContractNamesInput
        {
            ChainId = input.ChainId,
            ContractName = input.ContractName
        });

        if (!contractNamesIndices.IsNullOrEmpty())
        {
            return new CheckContractNameResponse
            {
                IsExist = true
            };
        }

        return new CheckContractNameResponse
        {
            IsExist = false
        };
    }
}