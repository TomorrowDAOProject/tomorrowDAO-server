using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly SenderAccount _senderAccount;
    private readonly List<QueryContractInfo> _queryContractInfos;

    public TopProposalGenerateService(ILogger<TopProposalGenerateService> logger, IGraphQLProvider graphQlProvider,
        IProposalProvider proposalProvider, IChainAppService chainAppService,IOptionsSnapshot<QueryContractOption> queryContractOption, 
        IOptionsMonitor<RankingOptions> rankingOptions, IContractProvider contractProvider, IRankingAppProvider rankingAppProvider,
        ITelegramAppsProvider telegramAppsProvider)
        : base(logger, graphQlProvider)
    {
        _logger = logger;
        _proposalProvider = proposalProvider;
        _chainAppService = chainAppService;
        _rankingOptions = rankingOptions;
        _contractProvider = contractProvider;
        _rankingAppProvider = rankingAppProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _senderAccount = new SenderAccount(rankingOptions.CurrentValue.TopRankingAccount);
        _queryContractInfos = queryContractOption.Value.QueryContractInfoList?.ToList() ?? new List<QueryContractInfo>();
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        if (!CheckDate(out var nextWeekStartTime, out var nextWeekEndTime))
        {
            return newIndexHeight;
        }
        
        var topRankingAddress = _rankingOptions.CurrentValue.TopRankingAddress;
        var proposalTitle = _rankingOptions.CurrentValue.TopRankingTitle;
        var schemeAddress = _rankingOptions.CurrentValue.TopRankingSchemeAddress;
        var voteSchemeId = _rankingOptions.CurrentValue.TopRankingVoteSchemeId;
        var banner = _rankingOptions.CurrentValue.TopRankingBanner;
        var daoId = _rankingOptions.CurrentValue.DaoIds[0];
        var queryContractInfo = _queryContractInfos.First(x => x.ChainId == chainId);
        var governanceContractAddress = queryContractInfo.GovernanceContractAddress;

        var proposal = await _proposalProvider.GetTopProposalAsync(topRankingAddress, false);
        if (proposal == null || proposal.ActiveStartTime != nextWeekStartTime)
        {
            var rankingList = await _rankingAppProvider.GetAllPeriodListAsync();
            var excludeAliasList = rankingList.Select(x => x.Alias).Distinct().ToList();
            var appList = await _telegramAppsProvider.GetAllDisplayAsync(excludeAliasList);
            if (appList.Count < 15)
            {
                appList = await _telegramAppsProvider.GetAllDisplayAsync(new List<string>());
            }
            var random = new Random();
            var randomList = appList.OrderBy(x => random.Next()).Take(15).ToList();
            var aliasList = randomList.Select(x => x.Alias).ToList();
            var proposalDescription = RankHelper.BuildProposalDescription(aliasList, banner);
            var (transactionId, transaction) = await _contractProvider.CreateTransactionAsync(chainId, _senderAccount.PublicKey.ToHex(), governanceContractAddress, 
                CommonConstant.GovernanceMethodCreateProposal, new CreateProposalInput
                {
                    ProposalType = 2,
                    ProposalBasicInfo = new ProposalBasicInfo
                    {
                        ProposalTitle = proposalTitle, ProposalDescription = proposalDescription, SchemeAddress = Address.FromBase58(schemeAddress), 
                        ActiveStartTime = nextWeekStartTime.Millisecond, ActiveEndTime = nextWeekEndTime.Millisecond, DaoId = Hash.LoadFromHex(daoId),
                        VoteSchemeId = Hash.LoadFromHex(voteSchemeId)
                    }
                });
            transaction.Signature = _senderAccount.GetSignatureWith(transaction.GetHash().ToByteArray());
            await _contractProvider.SendTransactionAsync(chainId, transaction);
        }
        return newIndexHeight;
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