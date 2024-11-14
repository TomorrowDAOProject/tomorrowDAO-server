using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aetherlink.PriceServer.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TomorrowDAOServer.Chains;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.TonGift.Provider;
using TomorrowDAOServer.User;
using TomorrowDAOServer.Vote;
using TomorrowDAOServer.Vote.Provider;

namespace TomorrowDAOServer.TonGift;

public class TonGiftTaskGenerateService : ScheduleSyncDataService
{
    private readonly ILogger<TonGiftTaskGenerateService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IVoteProvider _voteProvider;
    private readonly IOptionsMonitor<TonGiftTaskOptions> _tonGiftTaskOptions;
    private readonly ITonGiftTaskProvider _tonGiftTaskProvider;
    private readonly IPortkeyProvider _portkeyProvider;
    
    public TonGiftTaskGenerateService(ILogger<TonGiftTaskGenerateService> logger, IGraphQLProvider graphQlProvider, 
        IChainAppService chainAppService, IVoteProvider voteProvider, IOptionsMonitor<TonGiftTaskOptions> tonGiftTaskOptions, 
        ITonGiftTaskProvider tonGiftTaskProvider, IPortkeyProvider portkeyProvider) 
        : base(logger, graphQlProvider)
    {
        _chainAppService = chainAppService;
        _voteProvider = voteProvider;
        _tonGiftTaskOptions = tonGiftTaskOptions;
        _tonGiftTaskProvider = tonGiftTaskProvider;
        _portkeyProvider = portkeyProvider;
        _logger = logger;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var start = _tonGiftTaskOptions.CurrentValue.IsStart;
        if (!start)
        {
            _logger.LogInformation("TonGiftTaskNotStart");
            return lastEndHeight;
        }
        
        var proposalId = _tonGiftTaskOptions.CurrentValue.ProposalId;
        var taskId = _tonGiftTaskOptions.CurrentValue.TaskId;
        var skipCount = 0;
        var blockHeight = lastEndHeight;
        List<VoteRecordIndex> queryList;
        do
        {
            queryList = await _voteProvider.GetByProposalIdAndHeightAsync(proposalId, blockHeight, skipCount, CommonConstant.MaxResultCount);
            _logger.LogInformation("TonGiftTaskComplete queryList skipCount {skipCount} startBlockHeight: {lastEndHeight} count: {count}",
                skipCount, lastEndHeight, queryList?.Count);
            if (queryList == null || queryList.IsNullOrEmpty())
            {
                break;
            }

            var voters = queryList.Select(x => x.Voter).Distinct().ToList();
            var caHolderInfos = await _portkeyProvider.GetCaHolderInfoAsync(voters, null, 0, voters.Count);
            var caHashDic = caHolderInfos.CaHolderInfo
                .ToDictionary(holder => holder.CaAddress, holder => new Info { CaHash = holder.CaHash, OriginChainId = holder.OriginChainId });
            var tasks = caHashDic.Values.Select(async info =>
            {
                var guardianIdentifierList = await _portkeyProvider.GetGuardianIdentifiersAsync(info.CaHash, info.OriginChainId);
                info.Guardians = guardianIdentifierList.Guardians;
            });
            await Task.WhenAll(tasks);

            var toAdd = new List<TonGiftTaskIndex>();
            foreach (var voter in voters)
            {
                var info = caHashDic.GetValueOrDefault(voter, new Info());
                toAdd.AddRange(from guardian in info.Guardians
                    let identifier = guardian.Identifier
                    let identifierHash = guardian.IdentifierHash
                    select new TonGiftTaskIndex
                    {
                        Id = IdGeneratorHelper.GenerateId(taskId, voter, identifier),
                        TaskId = taskId, Address = voter, CaHash = info.CaHash,
                        Identifier = identifier, IdentifierHash = identifierHash,
                        TonGiftTask = TonGiftTask.Vote, UpdateTaskStatus = UpdateTaskStatus.Pending
                    });
            }

            var idList = toAdd.Select(x => x.Id).Distinct().ToList();
            var exists = await _tonGiftTaskProvider.GetByIdList(idList);
            var existIds = new HashSet<string>(exists.Select(e => e.Id));
            toAdd = toAdd.Where(item => !existIds.Contains(item.Id)).ToList();
            await _tonGiftTaskProvider.BulkAddOrUpdateAsync(toAdd);
            blockHeight = Math.Max(blockHeight, queryList.Select(t => t.BlockHeight).Max());
            skipCount += queryList.Count;
        } while (!queryList.IsNullOrEmpty());
        
        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override WorkerBusinessType GetBusinessType()
    {
        return WorkerBusinessType.TonGiftTaskGenerate;
    }

    private class Info
    {
        public string CaHash { get; set; } = string.Empty;
        public string OriginChainId { get; set; } = string.Empty;
        public List<GuardianIdentifier> Guardians { get; set; } = new();
    }
}