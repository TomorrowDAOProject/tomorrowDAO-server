using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Treasury;
using TomorrowDAOServer.Treasury.Dto;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.DAO;

public interface IDaoTreasuryAssetsAmountRefreshService
{
    Task RefreshDaoTreasuryAssetsAmount(string chainId, ISet<string> symbols);
}

public class DaoTreasuryAssetsAmountRefreshService : IDaoTreasuryAssetsAmountRefreshService, ISingletonDependency
{
    private readonly ILogger<DaoTreasuryAssetsAmountRefreshService> _logger;
    private readonly ITreasuryAssetsService _assetsService;
    private readonly IDAOProvider _daoProvider;
    private readonly INESTRepository<DAOIndex, string> _daoIndexRepository;

    private const int ResultCount = 100;

    public DaoTreasuryAssetsAmountRefreshService(ILogger<DaoTreasuryAssetsAmountRefreshService> logger,
        ITreasuryAssetsService assetsService, IDAOProvider daoProvider,
        INESTRepository<DAOIndex, string> daoIndexRepository)
    {
        _logger = logger;
        _assetsService = assetsService;
        _daoProvider = daoProvider;
        _daoIndexRepository = daoIndexRepository;
    }

    public async Task RefreshDaoTreasuryAssetsAmount(string chainId, ISet<string> symbols)
    {
        _logger.LogInformation("Refresh DAO treasury assets amount, chainId={0}, symbols={1}",
            chainId, JsonConvert.SerializeObject(symbols));

        var queryDaoListInput = new QueryDAOListInput
        {
            ChainId = chainId,
            SkipCount = 0,
            MaxResultCount = ResultCount,
        };
        
        var tokenInfo = await _assetsService.GetTokenInfoAsync(chainId, symbols);
        if (tokenInfo.Item1.IsNullOrEmpty() || tokenInfo.Item2.IsNullOrEmpty())
        {
            _logger.LogWarning("The symbol {0} token information does not exist.", JsonConvert.SerializeObject(symbols));
            return;
        }

        List<DAOIndex> daoList = null;
        do
        {
            var (totalCount, data) = await _daoProvider.GetDAOListAsync(queryDaoListInput, null);
            daoList = data;

            _logger.LogInformation("Processed DAO count: {0}", daoList.IsNullOrEmpty() ? 0 : daoList.Count);

            if (daoList.IsNullOrEmpty())
            {
                break;
            }

            foreach (var daoIndex in daoList)
            {
                var totalAmount = await _assetsService.GetTreasuryAssetsAmountAsync(new GetTreasuryAssetsInput
                {
                    MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                    SkipCount = 0,
                    DaoId = daoIndex.Id,
                    ChainId = daoIndex.ChainId,
                    Symbols = symbols
                }, tokenInfo);
                daoIndex.TreasuryDollarValue = totalAmount;
            }

            await _daoIndexRepository.BulkAddOrUpdateAsync(daoList);
            queryDaoListInput.SkipCount += daoList.Count;
        } while (!daoList.IsNullOrEmpty() && daoList!.Count == ResultCount);
    }
}