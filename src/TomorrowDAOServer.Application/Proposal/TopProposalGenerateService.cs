using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans.Runtime;
using TomorrowDAO.Contracts.Governance;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.AElfSdk.Dtos;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Provider;

namespace TomorrowDAOServer.Proposal;

public class TopProposalGenerateService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IProposalProvider _proposalProvider;
    private readonly IChainAppService _chainAppService;
    private readonly IContractProvider _contractProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly SenderAccount _senderAccount;

    public TopProposalGenerateService(ILogger<TopProposalGenerateService> logger, IGraphQLProvider graphQlProvider,
        IProposalProvider proposalProvider, IChainAppService chainAppService,
        IOptionsMonitor<RankingOptions> rankingOptions, IContractProvider contractProvider,
        IRankingAppProvider rankingAppProvider,
        ITelegramAppsProvider telegramAppsProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _chainAppService = chainAppService;
        _rankingOptions = rankingOptions;
        _contractProvider = contractProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _senderAccount = new SenderAccount(rankingOptions.CurrentValue.TopRankingAccount);
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        _logger.LogInformation("[TopProposalGenerate] start...");
        if (!CheckDate(out var nextWeekStartTime, out var nextWeekEndTime))
        {
            _logger.LogInformation("[TopProposalGenerate] wrong time...");
            return newIndexHeight;
        }

        var topRankingAddress = _rankingOptions.CurrentValue.TopRankingAddress;
        var proposalTitle = _rankingOptions.CurrentValue.TopRankingTitle;
        var schemeAddress = _rankingOptions.CurrentValue.TopRankingSchemeAddress;
        var voteSchemeId = _rankingOptions.CurrentValue.TopRankingVoteSchemeId;
        var banner = _rankingOptions.CurrentValue.TopRankingBanner;
        var daoId = _rankingOptions.CurrentValue.DaoIds[0];

        var proposal = await _proposalProvider.GetTopProposalAsync(topRankingAddress, false);
        _logger.LogInformation("[TopProposalGenerate] proposal:{0}", JsonConvert.SerializeObject(proposal));
        if (proposal == null || proposal.ActiveStartTime != nextWeekStartTime)
        {
            var excludeAliasList = await GetExcludeAliasListAsync();
            _logger.LogInformation("[TopProposalGenerate] excludeAliasList.count:{0}", excludeAliasList.Count);
            var appList = await _telegramAppsProvider.GetAllDisplayAsync(excludeAliasList);
            if (appList.Count < 15)
            {
                appList = await _telegramAppsProvider.GetAllDisplayAsync(new List<string>());
            }

            _logger.LogInformation("[TopProposalGenerate] appList.count:{0}", appList.Count);
            var random = new Random();
            var randomList = appList.OrderBy(x => random.Next()).Take(15).ToList();
            var aliasList = randomList.Select(x => x.Alias).ToList();
            _logger.LogInformation("[TopProposalGenerate] aliasList:{0}", JsonConvert.SerializeObject(aliasList));
            var proposalDescription = RankHelper.BuildProposalDescription(aliasList, banner);
            var (transactionId, transaction) = await _contractProvider.CreateTransactionAsync(chainId,
                _senderAccount.PublicKey.ToHex(), CommonConstant.GovernanceContractAddress,
                CommonConstant.GovernanceMethodCreateProposal, new CreateProposalInput
                {
                    ProposalType = 2,
                    ProposalBasicInfo = new ProposalBasicInfo
                    {
                        ProposalTitle = proposalTitle, ProposalDescription = proposalDescription,
                        SchemeAddress = Address.FromBase58(schemeAddress),
                        ActiveStartTime = TimeHelper.GetTimeStampFromDateTimeInSeconds(nextWeekStartTime),
                        ActiveEndTime = TimeHelper.GetTimeStampFromDateTimeInSeconds(nextWeekEndTime),
                         
                        DaoId = Hash.LoadFromHex(daoId),
                        VoteSchemeId = Hash.LoadFromHex(voteSchemeId)
                    }
                });
            var transactionIdString = transactionId.ToHex();
            _logger.LogInformation("[TopProposalGenerate] transactionId:{0}", transactionIdString);
            transaction.Signature = _senderAccount.GetSignatureWith(transaction.GetHash().ToByteArray());
            await _contractProvider.SendTransactionAsync(chainId, transaction);
            _logger.LogInformation("[TopProposalGenerate] send transaction success...");
            await QueryTransactionResultAsync(chainId, transactionIdString);
        }

        return newIndexHeight;
    }

    private async Task QueryTransactionResultAsync(string chainId, string transactionId)
    {
        try
        {
            var transactionResult = await _contractProvider.QueryTransactionResultAsync(transactionId, chainId);
            var times = 0;
            while (transactionResult.Status is CommonConstant.TransactionStatePending
                       or CommonConstant.TransactionStateNotExisted
                   && times < _rankingOptions.CurrentValue.RetryTimes)
            {
                times++;
                await Task.Delay(_rankingOptions.CurrentValue.RetryDelay);
                transactionResult = await _contractProvider.QueryTransactionResultAsync(transactionId, chainId);
            }

            _logger.LogInformation("[TopProposalGenerate] transactionResult={0}",
                JsonConvert.SerializeObject(transactionResult));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[TopProposalGenerate] error.");
        }
    }

    private async Task<List<string>> GetExcludeAliasListAsync()
    {
        var rankingList =
            await _proposalProvider.GetActiveRankingProposalListAsync(_rankingOptions.CurrentValue.DaoIds);
        if (rankingList.IsNullOrEmpty())
        {
            return new List<string>();
        }

        var aliasList = new List<string>();
        foreach (var proposalIndex in rankingList)
        {
            aliasList.AddRange(RankHelper.GetAliases(proposalIndex.ProposalDescription));
        }

        return aliasList;
    }

    private bool CheckDate(out DateTime nextWeekStartTime, out DateTime nextWeekEndTime)
    {
        nextWeekStartTime = default;
        nextWeekEndTime = default;
        var utcNow = DateTime.UtcNow;
        if (utcNow.DayOfWeek != _rankingOptions.CurrentValue.TopRankingGenerateTime)
        {
            return false;
        }

        nextWeekStartTime = utcNow.GetNextWeekday(DayOfWeek.Monday).Date;
        nextWeekEndTime = utcNow.GetNextWeekday(DayOfWeek.Sunday).Date.AddDays(1).AddMilliseconds(-1);
        return true;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.TopProposalGenerate;
    }
}