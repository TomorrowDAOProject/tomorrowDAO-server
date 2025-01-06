using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.ExceptionHandler;
using AElf.ExceptionHandler;
using AElf.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Serilog;
using TomorrowDAO.Contracts.Vote;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.Handler;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Digi.Provider;
using TomorrowDAOServer.Discover.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.LuckyBox.Provider;
using TomorrowDAOServer.MQ;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.Token.Provider;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Provider;
using TomorrowDAOServer.Vote;
using TomorrowDAOServer.Vote.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Ranking;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class RankingAppService : TomorrowDAOServerAppService, IRankingAppService
{
    private readonly ILogger<RankingAppService> _logger;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IProposalProvider _proposalProvider;
    private readonly IUserProvider _userProvider;
    private readonly IUserAppService _userAppService;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IContractProvider _contractProvider;
    private readonly ITransferTokenProvider _transferTokenProvider;
    private readonly IDAOProvider _daoProvider;
    private readonly IVoteProvider _voteProvider;
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IMessagePublisherService _messagePublisherService;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;
    private readonly IReferralInviteProvider _referralInviteProvider;
    private readonly IPortkeyProvider _portkeyProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly IUserPointsRecordProvider _userPointsRecordProvider;
    private readonly IDiscoverChoiceProvider _discoverChoiceProvider;
    private readonly IRankingAppPointsProvider _rankingAppPointsProvider;
    private readonly IUserViewAppProvider _userViewAppProvider;
    private readonly IOptionsMonitor<LuckyboxOptions> _luckyboxOptions;
    private readonly ILuckboxTaskProvider _luckboxTaskProvider;
    private readonly IOptionsMonitor<DigiOptions> _digiOptions;
    private readonly IDigiTaskProvider _digiTaskProvider;

    public RankingAppService(IRankingAppProvider rankingAppProvider, ITelegramAppsProvider telegramAppsProvider,
        IObjectMapper objectMapper, IProposalProvider proposalProvider, IUserProvider userProvider,
        IOptionsMonitor<RankingOptions> rankingOptions, IAbpDistributedLock distributedLock,
        ILogger<RankingAppService> logger, IContractProvider contractProvider,
        IDistributedCache<string> distributedCache, ITransferTokenProvider transferTokenProvider,
        IDAOProvider daoProvider, IVoteProvider voteProvider,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider,
        IMessagePublisherService messagePublisherService,
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider,
        IOptionsMonitor<TelegramOptions> telegramOptions, IReferralInviteProvider referralInviteProvider,
        IUserAppService userAppService, IPortkeyProvider portkeyProvider, IUserBalanceProvider userBalanceProvider,
        IUserPointsRecordProvider userPointsRecordProvider, IDiscoverChoiceProvider discoverChoiceProvider, 
        IRankingAppPointsProvider rankingAppPointsProvider, IUserViewAppProvider userViewAppProvider, 
        IOptionsMonitor<LuckyboxOptions> luckyboxOptions, ILuckboxTaskProvider luckboxTaskProvider,
        IOptionsMonitor<DigiOptions> digiOptions, IDigiTaskProvider digiTaskProvider)
    {
        _rankingAppProvider = rankingAppProvider;
        _telegramAppsProvider = telegramAppsProvider;
        _objectMapper = objectMapper;
        _proposalProvider = proposalProvider;
        _userProvider = userProvider;
        _rankingOptions = rankingOptions;
        _distributedLock = distributedLock;
        _logger = logger;
        _contractProvider = contractProvider;
        _distributedCache = distributedCache;
        _transferTokenProvider = transferTokenProvider;
        _daoProvider = daoProvider;
        _messagePublisherService = messagePublisherService;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _telegramOptions = telegramOptions;
        _referralInviteProvider = referralInviteProvider;
        _userAppService = userAppService;
        _portkeyProvider = portkeyProvider;
        _userBalanceProvider = userBalanceProvider;
        _userPointsRecordProvider = userPointsRecordProvider;
        _discoverChoiceProvider = discoverChoiceProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
        _userViewAppProvider = userViewAppProvider;
        _luckyboxOptions = luckyboxOptions;
        _luckboxTaskProvider = luckboxTaskProvider;
        _digiOptions = digiOptions;
        _digiTaskProvider = digiTaskProvider;
        _voteProvider = voteProvider;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
    }

    public async Task GenerateRankingApp(string chainId, List<IndexerProposal> proposalList)
    {
        _logger.LogInformation("[ProposalSync] Generate Ranking start... {0}", proposalList.IsNullOrEmpty() ? 0 : proposalList.Count);
        foreach (var proposal in proposalList)
        {
            var telegramApps = new List<TelegramAppIndex>();
            var aliases = new List<string>();
            if (proposal.Proposer == _rankingOptions.CurrentValue.TopRankingAddress && RankHelper.IsTMARanking(proposal.ProposalDescription))
            {
                _logger.LogInformation("[ProposalSync] TMA Ranking, proposalId={0}", proposal.ProposalId);
                var allTelegramApps = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
                {
                    SourceTypes = new List<SourceType>() { SourceType.Telegram , SourceType.FindMini}
                });

                telegramApps = allTelegramApps.Where(x => !x.Url.IsNullOrWhiteSpace() &&
                                                          !x.LongDescription.IsNullOrWhiteSpace() && !x.BackScreenshots.IsNullOrEmpty() &&
                                                          !x.BackIcon.IsNullOrWhiteSpace() && !x.Categories.IsNullOrEmpty()).ToList();
                _logger.LogInformation("[ProposalSync] TMA Ranking App Count={0}, proposalId={1}", 
                    telegramApps.IsNullOrEmpty() ? 0 : telegramApps.Count, proposal.ProposalId);
                aliases = telegramApps.Select(t => t.Alias).Distinct().ToList();
            }
            else
            {
                _logger.LogInformation("[ProposalSync] Ranking, ProposalId={0}", proposal.ProposalId);
                aliases = RankHelper.GetAliases(proposal.ProposalDescription);
                telegramApps = (await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
                {
                    Aliases = aliases
                })).Item2;
                _logger.LogInformation("[ProposalSync] Ranking App Count={0}, ProposalId={1}",
                    telegramApps.IsNullOrEmpty() ? 0 : telegramApps.Count, proposal.ProposalId);
            }
            
            var distinctTelegramApps = telegramApps.GroupBy(app => app.Alias)
                .Select(group => group.First()).ToList();
            var aliasToTelegramApp = distinctTelegramApps.ToDictionary(t => t.Alias);
            var rankingApps = _objectMapper.Map<List<TelegramAppIndex>, List<RankingAppIndex>>(distinctTelegramApps);
            _logger.LogInformation("[ProposalSync] Ranking {0} App Count={1}", proposal.ProposalId, rankingApps.Count);
            foreach (var rankingApp in rankingApps)
            {
                rankingApp.TotalPoints = 0;
                rankingApp.TotalVotes = 0;
                rankingApp.TotalLikes = 0;
                
                _objectMapper.Map(proposal, rankingApp);
                rankingApp.Id =
                    GuidHelper.GenerateGrainId(proposal.ChainId, proposal.DAOId, proposal.Id, rankingApp.Alias);

                var indexOf = aliases.IndexOf(rankingApp.Alias);
                rankingApp.AppIndex = indexOf == -1 ? Int32.MaxValue : indexOf;

                if (aliasToTelegramApp.ContainsKey(rankingApp.Alias))
                {
                    if (!aliasToTelegramApp[rankingApp.Alias].BackIcon.IsNullOrWhiteSpace())
                    {
                        rankingApp.Icon = aliasToTelegramApp[rankingApp.Alias].BackIcon;
                    }

                    if (!aliasToTelegramApp[rankingApp.Alias].BackScreenshots.IsNullOrEmpty())
                    {
                        rankingApp.Screenshots = aliasToTelegramApp[rankingApp.Alias].BackScreenshots;
                    }
                }
            }

            if (!rankingApps.IsNullOrEmpty())
            {
                await _rankingAppProvider.BulkAddOrUpdateAsync(rankingApps);
            }
            else
            {
                _logger.LogWarning("[ProposalSync] Ranking {0} App is empty.", proposal.ProposalId);
            }
            
            _logger.LogInformation("[ProposalSync] Saved Ranking {0} App Count={1}", proposal.ProposalId, rankingApps.Count);
        }

        if (!proposalList.IsNullOrEmpty())
        {
            //TODO Useless code
            var defaultRankingProposal = proposalList
                .Where(p => p.ActiveStartTime <= DateTime.UtcNow) 
                .MaxBy(p => p.DeployTime);
            if (defaultRankingProposal != null && !defaultRankingProposal.Id.IsNullOrWhiteSpace())
            {
                await _rankingAppPointsRedisProvider.GenerateRedisDefaultProposal(defaultRankingProposal.ProposalId,
                    defaultRankingProposal.ProposalDescription, chainId);
            }
        }
    }

    public async Task<RankingDetailDto> GetDefaultRankingProposalAsync(string chainId)
    {
        var userAddress = await _userProvider
            .GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var proposalId = await _rankingAppPointsRedisProvider.GetDefaultRankingProposalIdAsync(chainId);
        return await GetRankingProposalDetailAsync(userAddress, chainId, proposalId);
    }

    public async Task<RankingListPageResultDto<RankingListDto>> GetRankingProposalListAsync(GetRankingListInput input)
    {
        var chainId = input.ChainId;
        var userAddress = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        var excludeIds = new List<string>(_rankingOptions.CurrentValue.RankingExcludeIds);
        var (topRankingAddress, goldRankingId, topRankingIds) = await GetTopRankingIdsAsync();
        var rankingType = input.Type;
        var result = new Tuple<long, List<ProposalIndex>>(0, new List<ProposalIndex>());
        switch (rankingType)
        {
            case RankingType.Verified:
                var res = new List<ProposalIndex>();
                if (string.IsNullOrEmpty(goldRankingId))
                {
                    result = await _proposalProvider.GetRankingProposalListAsync(chainId, input.SkipCount,
                        input.MaxResultCount, rankingType, topRankingAddress, true, excludeIds);
                }
                else
                {
                    if (input.SkipCount == 0)
                    {
                        var goldProposal = await _proposalProvider.GetProposalByIdAsync(chainId, goldRankingId);
                        res.Add(goldProposal);
                        input.MaxResultCount -= 1;
                    }
                    else
                    {
                        input.SkipCount -= 1;
                    }
                    var officialProposals = await _proposalProvider.GetRankingProposalListAsync(chainId, input.SkipCount, input.MaxResultCount, rankingType, topRankingAddress, true, excludeIds);
                    res.AddRange(officialProposals.Item2);
                    result = new Tuple<long, List<ProposalIndex>>(officialProposals.Item1 + 1, res);
                }
                break;
            case RankingType.All:
                result = await _proposalProvider.GetRankingProposalListAsync(chainId, input.SkipCount, input.MaxResultCount, rankingType, string.Empty, false);
                break;
            case RankingType.Community:
                result = await _proposalProvider.GetRankingProposalListAsync(chainId, input.SkipCount, input.MaxResultCount, rankingType, string.Empty, false, excludeIds);
                break;
            case RankingType.Top:
                result = await _proposalProvider.GetRankingProposalListAsync(chainId, input.SkipCount, input.MaxResultCount, RankingType.Verified, string.Empty, true, excludeIds);
                break;
        }
        
        var list = ObjectMapper.Map<List<ProposalIndex>, List<RankingListDto>>(result.Item2);
        var descList = list.Where(x => !string.IsNullOrEmpty(x.ProposalDescription)).Select(x => x.ProposalDescription).ToList();
        var proposalIds = list.Select(x => x.ProposalId).ToList();
        var bannerDic = await GetBannerUrlsAsync(descList);
        var pointsList = await _rankingAppPointsProvider.GetByProposalIdsAndPointsType(proposalIds, PointsType.Vote);
        var pointsDic = pointsList.GroupBy(p => p.ProposalId).ToDictionary(t => t.Key, t => t.ToList());
        var utcNow = DateTime.UtcNow;
        foreach (var detail in list)
        {
            var pointsIndex = pointsDic.GetValueOrDefault(detail.ProposalId, new List<RankingAppPointsIndex>());
            detail.TotalVoteAmount = pointsIndex.Sum(t => t.Amount);
            detail.RankingType = detail.RankingType == RankingType.All ? RankingType.Verified : detail.RankingType;
            if (detail.RankingType == RankingType.Verified)
            {
                detail.LabelType = detail.ProposalId == goldRankingId ? LabelTypeEnum.Gold : LabelTypeEnum.Blue;
            }
            if (input.Type == RankingType.Top)
            {
                detail.RankingType = RankingType.Top;
            }
            detail.Active = utcNow >= detail.ActiveStartTime && utcNow <= detail.ActiveEndTime;
            detail.BannerUrl = string.IsNullOrEmpty(detail.ProposalDescription) ? string.Empty : bannerDic.GetValueOrDefault(detail.ProposalDescription, string.Empty);
        }
        if (input.Type != RankingType.Community)
        {
            list.Where(x => string.IsNullOrEmpty(x.BannerUrl) && x.Proposer == topRankingAddress)
                .ToList().ForEach(item => item.BannerUrl = _rankingOptions.CurrentValue.TopRankingBanner);
        }
        var userAllPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsByAddressAsync(userAddress);
        return new RankingListPageResultDto<RankingListDto>
        {
            TotalCount = result.Item1, Data = list, UserTotalPoints = userAllPoints
        };
    }

    public async Task<RankingListPageResultDto<RankingListDto>> GetPollListAsync(GetPollListInput input)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var active = input.Type == CommonConstant.Current;
        var excludeIds = new List<string>(_rankingOptions.CurrentValue.RankingExcludeIds);
        var result = await _proposalProvider.GetPollListAsync(input.ChainId, input.SkipCount, input.MaxResultCount, active, excludeIds);
        var list = ObjectMapper.Map<List<ProposalIndex>, List<RankingListDto>>(result.Item2);
        var descList = list.Where(x => !string.IsNullOrEmpty(x.ProposalDescription)).Select(x => x.ProposalDescription).ToList();
        var proposalIds = list.Select(x => x.ProposalId).ToList();
        var bannerDic = await GetBannerUrlsAsync(descList);
        var pointsList = await _rankingAppPointsProvider.GetByProposalIdsAndPointsType(proposalIds, PointsType.Vote);
        var pointsDic = pointsList.GroupBy(p => p.ProposalId).ToDictionary(t => t.Key, t => t.ToList());
        var utcNow = DateTime.UtcNow;
        foreach (var detail in list)
        {
            var pointsIndex = pointsDic.GetValueOrDefault(detail.ProposalId, new List<RankingAppPointsIndex>());
            detail.TotalVoteAmount = pointsIndex.Sum(t => t.Amount);
            detail.Tag = detail.RankingType == RankingType.Verified ? CommonConstant.Trending : string.Empty;
            detail.Active = utcNow >= detail.ActiveStartTime && utcNow <= detail.ActiveEndTime;
            detail.BannerUrl = string.IsNullOrEmpty(detail.ProposalDescription) ? string.Empty : bannerDic.GetValueOrDefault(detail.ProposalDescription, string.Empty);
        }
        var userAllPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userId, address);
        return new RankingListPageResultDto<RankingListDto>
        {
            Data = list, TotalCount = result.Item1,
            UserTotalPoints = userAllPoints,
        };
    }

    private async Task<Tuple<string, string, List<string>>> GetTopRankingIdsAsync()
    {
        var topRankingIds = new List<string>(_rankingOptions.CurrentValue.TopRankingIds);
        
        var topRankingAddress = _rankingOptions.CurrentValue.TopRankingAddress;
        var topProposal = await _proposalProvider.GetTopProposalAsync(topRankingAddress, true);

        // if (topProposal != null && !topRankingIds.Contains(topProposal.ProposalId))
        // {
        //     topRankingIds.Insert(0, topProposal.ProposalId);
        // }

        return new Tuple<string, string, List<string>>(topRankingAddress, topProposal?.ProposalId, topRankingIds);
    }

    public async Task<RankingVoteResponse> VoteAsync(RankingVoteInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.RawTransaction.IsNullOrWhiteSpace() || input.TransactionId.IsNullOrEmpty())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        Log.Information("Ranking vote, start...");
        // var (address, addressCaHash) =
        //     await _userProvider.GetAndValidateUserAddressAndCaHashAsync(
        //         CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input!.ChainId);
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var addressCaHash = userGrainDto.CaHash;
        var userId = userGrainDto.UserId.ToString();

        if (address.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("User Address Not Found.");
        }

        Log.Information("Ranking vote, parse rawTransaction. {0}", address);
        var (voteInput, transaction) = await ParseRawTransaction(input.ChainId, input.RawTransaction);
        var votingItemId = voteInput.VotingItemId.ToHex();
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, votingItemId);
        if (!IsVoteDuring(proposalIndex))
        {
            throw new UserFriendlyException("Can not vote now.");
        }

        Log.Information("Ranking vote, query voting record.{0}", address);
        var category = input.Category?.ToString() ?? string.Empty;
        var votingRecord = await GetRankingVoteRecordAsync(input.ChainId, address, votingItemId, category);
        var userTotalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userId, address);

        if (votingRecord != null)
        {
            Log.Information("Ranking vote, vote exist. {0}", address);
            return BuildRankingVoteResponse(votingRecord.Status, votingRecord.TransactionId, userTotalPoints);
        }

        IAbpDistributedLockHandle lockHandle = null;
        try
        {
            Log.Information("Ranking vote, lock. {0}", address);
            var distributedLockKey =
                RedisHelper.GenerateDistributedLockKey(input.ChainId, address, voteInput.VotingItemId?.ToHex());
            lockHandle = await _distributedLock.TryAcquireAsync(distributedLockKey,
                _rankingOptions.CurrentValue.GetLockUserTimeoutTimeSpan());
            {
                if (lockHandle == null)
                {
                    Log.Information("Ranking vote, lock failed. {0}", address);
                    return BuildRankingVoteResponse(RankingVoteStatusEnum.Failed, userTotalPoints: userTotalPoints);
                }

                Log.Information("Ranking vote, query voting record again.{0}", address);
                votingRecord = await GetRankingVoteRecordAsync(input.ChainId, address, votingItemId, category);
                if (votingRecord != null)
                {
                    Log.Information("Ranking vote, vote exist. {0}", address);
                    return BuildRankingVoteResponse(votingRecord.Status, votingRecord.TransactionId, userTotalPoints);
                }

                _logger.LogInformation("Ranking vote, send transaction. {0}", address);
                // var sendTransactionOutput = await _contractProvider.SendTransactionAsync(input.ChainId, transaction);
                // if (sendTransactionOutput.TransactionId.IsNullOrWhiteSpace())
                // {
                //     _logger.LogError("Ranking vote, send transaction error, {0}",
                //         JsonConvert.SerializeObject(sendTransactionOutput));
                //     return BuildRankingVoteResponse(RankingVoteStatusEnum.Failed);
                // }

                Log.Information("Ranking vote, send transaction success. {0}, TransactionId={1}",
                    address, input.TransactionId);
                await SaveVotingRecordAsync(input.ChainId, address, votingItemId, RankingVoteStatusEnum.Voting,
                    input.TransactionId, category, _rankingOptions.CurrentValue.GetVoteTimoutTimeSpan());

                var _ = UpdateVotingStatusAsync(input.ChainId, userId,address, votingItemId,
                    input.TransactionId, voteInput.Memo, voteInput.VoteAmount, addressCaHash,
                    proposalIndex, input.TrackId, category);

                return BuildRankingVoteResponse(RankingVoteStatusEnum.Voting, input.TransactionId, userTotalPoints);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Ranking vote, error. {0}", JsonConvert.SerializeObject(input));
            ExceptionHelper.ThrowSystemException("voting", e);
            return new RankingVoteResponse();
        }
        finally
        {
            if (lockHandle != null)
            {
                await lockHandle.DisposeAsync();
            }
        }
    }

    public async Task<RankingVoteRecord> GetVoteStatusAsync(GetVoteStatusInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.Address.IsNullOrWhiteSpace() ||
            input.ProposalId.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        var category = input.Category?.ToString() ?? string.Empty;
        var voteRecord = await GetRankingVoteRecordAsync(input!.ChainId, input.Address, input.ProposalId, category);
        if (voteRecord == null)
        {
            return new RankingVoteRecord
            {
                TransactionId = null,
                VoteTime = null,
                Status = RankingVoteStatusEnum.Failed
            };
        }

        if (voteRecord.Status == RankingVoteStatusEnum.Voted)
        {
            var points = await _rankingAppPointsRedisProvider.GetUserAllPointsByAddressAsync(input.Address);
            voteRecord.TotalPoints = points;
        }

        return voteRecord;
    }

    public async Task MoveHistoryDataAsync(string chainId, string type, string key, string value)
    {
        var address = await CheckAddress(chainId);
        Log.Information("MoveHistoryDataAsync address {address} chainId {chainId} type {type}", address, chainId,
            type);
        string searchValue;
        switch (type)
        {
            case "1":
                await VoteRecordToPointsRecord(chainId);
                break;
            case "2":
                await ReferralInviteToPointsRecord(chainId);
                break;
            case "3":
                await UpdateRankingAppInfo();
                break;
            case "4":
                await GetReferralInviteCountToGrain(chainId);
                break;
            case "5":
                await ReferralInviteCountToGrain(chainId);
                break;
            case "6":
                await VoteToCategory(chainId);
                break;
            case "9":
                searchValue = await _rankingAppPointsRedisProvider.GetAsync(key);
                Log.Information("RedisValue key {key} value {value}", key, searchValue);
                break;
            case "10":
                searchValue = await _distributedCache.GetAsync(key);
                Log.Information("RedisDistributedCacheValue key {key} value {value}", key, searchValue);
                break;
            case "11":
                await _rankingAppPointsRedisProvider.SetAsync(key, value);
                break;
        }
    }

    private async Task VoteRecordToPointsRecord(string chainId)
    {
        Log.Information("VoteRecordToPointsRecordBegin chainId {chainId}", chainId);
        var voteRecordList = await _voteProvider.GetNeedMoveVoteRecordListAsync();
        var proposalIdList = voteRecordList.Select(x => x.VotingItemId).Distinct().ToList();
        var proposalDic = (await _proposalProvider.GetProposalByIdsAsync(chainId, proposalIdList))
            .ToDictionary(x => x.ProposalId, x => x);
        var toAdd = new List<UserPointsIndex>();
        foreach (var voteRecord in voteRecordList)
        {
            var proposalId = voteRecord.VotingItemId;
            var proposalIndex = proposalDic.GetValueOrDefault(proposalId);
            var id = GuidHelper.GenerateGrainId(chainId, UserTask.Daily, UserTaskDetail.DailyVote, PointsType.Vote, voteRecord.Voter,
                proposalId, voteRecord.VoteTime.ToUtcString(TimeHelper.DatePattern));
            var points = _rankingAppPointsCalcProvider.CalculatePointsFromVotes(1);
            toAdd.Add(new UserPointsIndex
            {
                Id = id, ChainId = chainId, Address = voteRecord.Voter, 
                Information = InformationHelper.GetDailyVoteInformation(proposalIndex, voteRecord.Alias),
                UserTask = UserTask.Daily, UserTaskDetail = UserTaskDetail.DailyVote,
                PointsType = PointsType.Vote, Points = points, PointsTime = voteRecord.VoteTime
            });
        }

        await _userPointsRecordProvider.BulkAddOrUpdateAsync(toAdd);
        Log.Information("VoteRecordToPointsRecordEnd chainId {chainId} count {count}", chainId, toAdd.Count);
    }

    private async Task ReferralInviteToPointsRecord(string chainId)
    {
        Log.Information("ReferralInviteToPointsRecordBegin chainId {chainId}", chainId);
        var invitePair = _rankingOptions.CurrentValue.ReferralPointsAddressList;
        var inviter = invitePair[0];
        var invitee = invitePair[1];
        var voteRecord = (await _voteProvider.GetByVoterAndVotingItemIdsAsync(chainId, invitee, null))
            .Where(vote => vote.ValidRankingVote).MinBy(vote => vote.VoteTime);
        if (voteRecord != null)
        {
            await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, inviter, UserTaskDetail.None, PointsType.InviteVote, voteRecord.VoteTime, 
                InformationHelper.GetInviteVoteInformation(invitee));
            await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, invitee, UserTaskDetail.None, PointsType.BeInviteVote, voteRecord.VoteTime,
                InformationHelper.GetBeInviteVoteInformation(inviter));
        }
        Log.Information("ReferralInviteToPointsRecordEnd chainId {chainId} voteRecordIsNull {voteRecordIsNull}", 
            chainId, voteRecord == null);
    }
    
    private async Task GetReferralInviteCountToGrain(string chainId)
    {
        Log.Information("GetReferralInviteCountToGrainBegin chainId {chainId}", chainId);
        var inviter = _rankingOptions.CurrentValue.ReferralPointsAddressList[0];
        var count = await _referralInviteProvider.GetInviteCountAsync(chainId, inviter);
        Log.Information("GetReferralInviteCountToGrainEnd chainId {chainId} count {count}", chainId, count);
    }

    private async Task ReferralInviteCountToGrain(string chainId)
    {
        Log.Information("ReferralInviteCountToGrainBegin chainId {chainId}", chainId);
        var inviter = _rankingOptions.CurrentValue.ReferralPointsAddressList[0];
        await _referralInviteProvider.IncrementInviteCountAsync(chainId, inviter, 1);
        Log.Information("ReferralInviteCountToGrainEnd chainId {chainId}", chainId);
    }

    private async Task VoteToCategory(string chainId)
    {
        // Log.LogInformation("VoteToCategoryBegin chainId {chainId}", chainId);
        // var appList = await _telegramAppsProvider.GetAllAsync();
        // var voteRecordList = await _voteProvider.GetNeedMoveVoteRecordListAsync();
        // var appDictionary = appList.ToDictionary(app => app.Alias, app => app.Categories);
        // var voterCategoryDic = voteRecordList
        //     .Where(record => !string.IsNullOrEmpty(record.Alias))
        //     .GroupBy(record => record.Voter)
        //     .ToDictionary(
        //         group => group.Key,
        //         group => group
        //             .SelectMany(record => appDictionary.TryGetValue(record.Alias, out var categories) 
        //                 ? categories 
        //                 : Enumerable.Empty<TelegramAppCategory>())
        //             .Distinct()
        //             .ToList()
        //     );
        // var toAdd = new List<DiscoverChoiceIndex>();
        // foreach (var (voter, categories) in voterCategoryDic)
        // {
        //     toAdd.AddRange(categories.Select(category => new DiscoverChoiceIndex
        //     {
        //         Id = GuidHelper.GenerateGrainId(chainId, voter, category.ToString(), DiscoverChoiceType.Vote.ToString()),
        //         ChainId = chainId,
        //         Address = voter,
        //         TelegramAppCategory = category,
        //         DiscoverChoiceType = DiscoverChoiceType.Vote,
        //         UpdateTime = DateTime.UtcNow
        //     }));
        // }
        //
        // await _discoverChoiceProvider.BulkAddOrUpdateAsync(toAdd);
        // Log.LogInformation("VoteToCategoryEnd chainId {chainId} count {count}", chainId, toAdd.Count);
    }

    private async Task UpdateRankingAppInfo()
    {
        //update RankingAppIndex url、LongDescription、Screenshots
        var historyAppVotes = await _rankingAppProvider.GetNeedMoveRankingAppListAsync();
        var aliasesList = historyAppVotes.Select(rankingAppIndex => rankingAppIndex.Alias).ToList();
        var telegramApps = await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = aliasesList
        });
        if (!telegramApps.Item2.IsNullOrEmpty())
        {
            var telegramAppMap = telegramApps.Item2.ToDictionary(item => item.Alias);
            foreach (var rankingAppIndex in historyAppVotes.Where(rankingAppIndex => telegramAppMap.ContainsKey(rankingAppIndex.Alias)))
            {
                rankingAppIndex.Url = telegramAppMap[rankingAppIndex.Alias].Url;
                rankingAppIndex.LongDescription = telegramAppMap[rankingAppIndex.Alias].LongDescription;
                rankingAppIndex.Screenshots = telegramAppMap[rankingAppIndex.Alias].Screenshots;
            }

            await _rankingAppProvider.BulkAddOrUpdateAsync(historyAppVotes);
        }
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultThrowMethodName)]
    public virtual async Task<RankingAppLikeResultDto> LikeAsync(RankingAppLikeInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.LikeList.IsNullOrEmpty())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(input.ChainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();

        if (!input.ProposalId.IsNullOrWhiteSpace())
        {
            var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, input.ProposalId);
            if (proposalIndex == null)
            {
                throw new UserFriendlyException($"Cannot be liked.{input.ProposalId}");
            }
        }

        var likePointsDic = await _rankingAppPointsRedisProvider.IncrementLikePointsAsync(input, address.IsNullOrWhiteSpace() ? userId : address);

        var _ = _messagePublisherService.SendLikeMessageAsync(input.ChainId, input.ProposalId, address, input.LikeList, userId);

        var userTotalPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userId, address);
        return new RankingAppLikeResultDto
        {
            UserTotalPoints = userTotalPoints,
            AppLikeCount = likePointsDic
        };
    }

    public async Task<RankingActivityResultDto> GetRankingActivityResultAsync(string chainId, string proposalId, int count)
    {
        await CheckAddress(chainId);
        var voters = (await _voteProvider.GetDistinctVotersAsync(proposalId)).Distinct().ToList();
        var voterKeyMap = voters.ToDictionary(voter => voter, RedisHelper.GenerateUserPointsAllCacheKey);
        var keys = voterKeyMap.Values.ToList();
        var groupCount = _rankingOptions.CurrentValue.GroupCount;
        var groupedKeys = keys.Select((key, index) => new { key, index })
            .GroupBy(x => x.index / groupCount)
            .Select(g => g.Select(x => x.key).ToList())
            .ToList();
        var allPoints = new Dictionary<string, long>();
        foreach (var keyGroup in groupedKeys)
        {
            var partialResults = await _rankingAppPointsRedisProvider.MultiGetAsync(keyGroup);
            foreach (var (key, points) in partialResults)
            {
                if (long.TryParse(points, out var pointsLong))
                {
                    allPoints[key] = pointsLong;
                }
            }
        }
        var voterPointsList = voters.Select(voter => 
        {
            var key = voterKeyMap[voter];
            return (Voter: voter, Points: allPoints.TryGetValue(key, out var points) ? points : 0L);
        }).ToList();
        var sortedVoters = voterPointsList.OrderByDescending(vp => vp.Points).Take((int)count).ToList();
        var resultDto = new RankingActivityResultDto
        {
            Data = sortedVoters.Select((vp, index) => new RankingActivityUserInfotDto
            {
                Rank = index + 1,
                Address = vp.Voter,
                Points = vp.Points
            }).ToList()
        };
    
        return resultDto;
    }

    public async Task<RankingBannerInfo> GetBannerInfoAsync(string chainId)
    {
        var userGrainDto = await _userProvider.GetAuthenticatedUserAsync(CurrentUser);
        var address = await _userProvider.GetUserAddressAsync(chainId, userGrainDto);
        var userId = userGrainDto.UserId.ToString();
        var hasFire = _rankingOptions.CurrentValue.TopRankingIds.Count > 0;
        var notViewedNewAppCount = 0;
        var latest = await _telegramAppsProvider.GetLatestCreatedAsync();
        if (latest != null)
        {
            var createTime = latest.CreateTime;
            var start = createTime.AddDays(-30);
            var newAppList = (await _telegramAppsProvider.GetAllByTimePeriodAsync(start, createTime))
                .OrderByDescending(x => x.CreateTime).Take(100).ToList();
            var aliases = newAppList.Select(x => x.Alias).Distinct().ToList();
            var viewedApps = await _userViewAppProvider.GetByAliasList(userId, address, aliases);
            var viewedAliases = viewedApps.Select(x => x.Alias).ToList();
            notViewedNewAppCount = aliases.Except(viewedAliases).Count();
        }
        return new RankingBannerInfo
        {
            HasFire = hasFire, NotViewedNewAppCount = notViewedNewAppCount
        };
    }

    private async Task SaveVotingRecordAsync(string chainId, string address,
        string proposalId, RankingVoteStatusEnum status, string transactionId, string category, TimeSpan? expire = null)
    {
        var distributeCacheKey = RedisHelper.GenerateDistributeCacheKey(chainId, address, proposalId, category);
        await _distributedCache.SetAsync(distributeCacheKey, JsonConvert.SerializeObject(new RankingVoteRecord
            {
                TransactionId = transactionId,
                VoteTime = DateTime.Now.ToUtcString(TimeHelper.DefaultPattern),
                Status = status
            }),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expire ?? GetCacheExpireTimeSpan(),
            });
    }

    public async Task<RankingDetailDto> GetRankingProposalDetailAsync(string userAddress, string chainId, string proposalId)
    {
        var userAllPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsByAddressAsync(userAddress);
        if (proposalId.IsNullOrEmpty())
        {
            return new RankingDetailDto { UserTotalPoints = userAllPoints };
        }

        var rankingAppList = await _rankingAppProvider.GetByProposalIdAsync(chainId, proposalId);
        var proposal = await _proposalProvider.GetProposalByIdAsync(chainId, proposalId);
        if (rankingAppList.IsNullOrEmpty() || proposal == null)
        {
            return new RankingDetailDto { UserTotalPoints = userAllPoints };
        }

        var labelType = LabelTypeEnum.None;
        var proposer = proposal.Proposer;
        var (_, goldRankingId, topRankingIds) = await GetTopRankingIdsAsync();
        var rankingApp = rankingAppList[0];
        var canVoteAmount = 0;
        var proposalDescription = rankingApp.ProposalDescription;
        var getBannerUrlTask = GetBannerUrlAsync(chainId, proposalDescription);
        var utcNow = DateTime.UtcNow;
        if (utcNow >= rankingApp.ActiveStartTime && utcNow <= rankingApp.ActiveEndTime)
        {
            //TODO Default Ranking, query voting in each category
            var voteRecordRedis = await GetRankingVoteRecordAsync(chainId, userAddress, proposalId, string.Empty);
            if (voteRecordRedis is { Status: RankingVoteStatusEnum.Voted or RankingVoteStatusEnum.Voting })
            {
                canVoteAmount = 0;
            }
            else
            {
                var voteRecordEs = await GetRankingVoteRecordEsAsync(chainId, userAddress, proposalId);
                if (voteRecordEs == null)
                {
                    canVoteAmount = 1;
                    // var userBalance = await _userBalanceProvider.GetByIdAsync(GuidHelper.GenerateGrainId(userAddress,
                    //     chainId, CommonConstant.GetVotigramSymbol(chainId)));
                    // canVoteAmount = (userBalance?.Amount ?? 0) > 0 ? 1 : 0;
                }
            }
        }
        var aliasList = RankHelper.GetAliases(proposalDescription);
        var appPointsList = await _rankingAppPointsRedisProvider.GetAllAppPointsAsync(chainId, proposalId, aliasList);
        var appVoteAmountDic = appPointsList
            .Where(x => x.PointsType == PointsType.Vote)
            .ToDictionary(x => x.Alias, x => _rankingAppPointsCalcProvider.CalculateVotesFromPoints(x.Points));
        var totalVoteAmount = appVoteAmountDic.Values.Sum();
        var totalPoints = appPointsList.Sum(x => x.Points);
        var votePercentFactor = DoubleHelper.GetFactor(totalVoteAmount);
        var pointsPercentFactor = DoubleHelper.GetFactor((decimal)totalPoints);
        var appPointsDic = RankingAppPointsDto
            .ConvertToBaseList(appPointsList)
            .ToDictionary(x => x.Alias, x => x.Points);
        var rankingList = ObjectMapper.Map<List<RankingAppIndex>, List<RankingAppDetailDto>>(rankingAppList);
        foreach (var rankingAppDetailDto in rankingList)
        {
            var icon = rankingAppDetailDto.Icon;
            var needPrefix = !string.IsNullOrEmpty(icon) && icon.StartsWith("/");
            if (needPrefix)
            {
                rankingAppDetailDto.Icon = CommonConstant.FindminiUrlPrefix + icon;
            }
        }
        var rankingType = proposal.RankingType == RankingType.All ? RankingType.Verified : proposal.RankingType;
        if (rankingType == RankingType.Verified)
        {
            labelType = proposal.ProposalId == goldRankingId ? LabelTypeEnum.Gold : LabelTypeEnum.Blue;
        }
        if (topRankingIds.Contains(proposal.ProposalId))
        {
            rankingType = RankingType.Top;
        }
        foreach (var app in rankingList)
        {
            app.PointsAmount = appPointsDic.GetValueOrDefault(app.Alias, 0);
            app.VoteAmount = appVoteAmountDic.GetValueOrDefault(app.Alias, 0);
            app.VotePercent = appVoteAmountDic.GetValueOrDefault(app.Alias, 0) * votePercentFactor;
            app.PointsPercent = app.PointsAmount * pointsPercentFactor;
        }

        var bannerUrl = await getBannerUrlTask;
        if (string.IsNullOrEmpty(bannerUrl) && proposer == _rankingOptions.CurrentValue.TopRankingAddress)
        {
            bannerUrl = _rankingOptions.CurrentValue.TopRankingBanner;
        }
        return new RankingDetailDto
        {
            StartTime = rankingApp.ActiveStartTime,
            EndTime = rankingApp.ActiveEndTime,
            CanVoteAmount = canVoteAmount,
            TotalVoteAmount = totalVoteAmount,
            UserTotalPoints = userAllPoints,
            BannerUrl = bannerUrl,
            RankingType = rankingType,
            LabelType = labelType,
            ProposalTitle = proposal.ProposalTitle,
            RankingList = rankingList.OrderByDescending(r => r.PointsAmount)
                .ThenBy(r => r.Title).ToList(),
            StartEpochTime = rankingApp.ActiveStartTime.ToUtcMilliSeconds(),
            EndEpochTime = rankingApp.ActiveEndTime.ToUtcMilliSeconds(),
        };
    }

    private async Task<string> GetBannerUrlAsync(string chainId, string proposalDescription)
    {
        var banner = RankHelper.GetBanner(proposalDescription);
        if (banner.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        var (count, telegramAppIndices) = await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = new List<string>() {banner},
        });
        if (telegramAppIndices.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return telegramAppIndices.First().Icon ?? string.Empty;
    }
    
    private async Task<Dictionary<string, string>> GetBannerUrlsAsync(List<string> proposalDescriptions)
    {
        var dic = proposalDescriptions
            .GroupBy(desc => desc)
            .Select(group => new { group.Key, Banner = RankHelper.GetBanner(group.First()) })
            .Where(item => item.Banner != null && !string.IsNullOrEmpty(item.Banner))
            .ToDictionary(item => item.Key, item => item.Banner);
        var banners = dic.Values.ToList();

        var (_, telegramAppIndices) = await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
        {
            Aliases = banners,
        });
        
        if (telegramAppIndices.IsNullOrEmpty())
        {
            return new Dictionary<string, string>();
        }

        return dic.ToDictionary(
            pair => pair.Key,
            pair => telegramAppIndices.FirstOrDefault(t => t.Alias == pair.Value)?.Icon ?? string.Empty
        );
    }

    public async Task<RankingDetailDto> GetRankingProposalDetailAsync(string chainId, string proposalId)
    {
        var userAddress = await _userProvider
            .GetAndValidateUserAddressAsync(CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        return await GetRankingProposalDetailAsync(userAddress, chainId, proposalId);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleParseRawTransaction),
        Message = "VoteAsync error", LogTargets = new []{"chainId", "rawTransaction"})]
    public virtual async Task<Tuple<VoteInput, Transaction>> ParseRawTransaction(string chainId, string rawTransaction)
    {
        var bytes = ByteArrayHelper.HexStringToByteArray(rawTransaction);
        var transaction = Transaction.Parser.ParseFrom(bytes);

        VoteInput voteInput = null;
        var caAddress = _contractProvider.ContractAddress(chainId, CommonConstant.CaContractAddressName);
        var voteAddress = _contractProvider.ContractAddress(chainId, CommonConstant.VoteContractAddressName);
        if (transaction.To.ToBase58() == caAddress && transaction.MethodName == "ManagerForwardCall")
        {
            var managerForwardCallInput = Portkey.Contracts.CA.ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
            if (managerForwardCallInput.MethodName == "Vote" &&
                managerForwardCallInput.ContractAddress.ToBase58() == voteAddress)
            {
                voteInput = VoteInput.Parser.ParseFrom(managerForwardCallInput.Args);
            }
        }
        else if (transaction.To.ToBase58() == voteAddress && transaction.MethodName == "Vote")
        {
            voteInput = VoteInput.Parser.ParseFrom(transaction.Params);
        }

        if (voteInput == null)
        {
            ExceptionHelper.ThrowArgumentException();
        }

        return new Tuple<VoteInput, Transaction>(voteInput, transaction);
    }

    private RankingVoteResponse BuildRankingVoteResponse(RankingVoteStatusEnum status, string TranscationId = null, long userTotalPoints = 0)
    {
        return new RankingVoteResponse
        {
            Status = status,
            TransactionId = TranscationId,
            UserTotalPoints = userTotalPoints
        };
    }

    public async Task<RankingVoteRecord> GetRankingVoteRecordAsync(string chainId, string address, string proposalId, string category)
    {
        var distributeCacheKey = RedisHelper.GenerateDistributeCacheKey(chainId, address, proposalId, category);
        var cache = await _distributedCache.GetAsync(distributeCacheKey);
        return cache.IsNullOrWhiteSpace() ? null : JsonConvert.DeserializeObject<RankingVoteRecord>(cache);
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, ReturnDefault = default,
        Message = "GetRankingVoteRecordEsAsync error", LogTargets = new []{"chainId", "address", "proposalId"})]
    public virtual async Task<VoteRecordIndex> GetRankingVoteRecordEsAsync(string chainId, string address, string proposalId)
    {
        return (await _voteProvider.GetByVoterAndVotingItemIdsAsync(chainId, address,
                new List<string> { proposalId }))
            .Where(x => x.VoteTime.ToString(CommonConstant.DayFormatString) ==
                        DateTime.UtcNow.ToString(CommonConstant.DayFormatString))
            .ToList().SingleOrDefault();
    }

    private TimeSpan GetCacheExpireTimeSpan()
    {
        var nowUtc = DateTime.UtcNow;
        var nextDay = nowUtc.Date.AddDays(1);
        return nextDay - nowUtc;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, ReturnDefault = ReturnDefault.None,
        Message = "Ranking vote, update transaction status error", LogTargets = new []{"transactionId"})]
    public virtual async Task UpdateVotingStatusAsync(string chainId, string userId, string address, string votingItemId,
        string transactionId, string memo, long amount, string addressCaHash, ProposalIndex proposalIndex,
        string trackId, string category)
    {
        Log.Information("Ranking vote, update transaction status start.{0}", address);
        var transactionResult = await _contractProvider.QueryTransactionResultAsync(transactionId, chainId);
        _logger.LogInformation("Ranking vote, transaction status {0}, {1}", transactionId,
            JsonConvert.SerializeObject(transactionResult));
        var times = 0;
        while (transactionResult.Status is CommonConstant.TransactionStatePending or CommonConstant.TransactionStateNotExisted 
               && times < _rankingOptions.CurrentValue.RetryTimes)
        {
            times++;
            await Task.Delay(_rankingOptions.CurrentValue.RetryDelay);
            transactionResult = await _contractProvider.QueryTransactionResultAsync(transactionId, chainId);
        }

        if (transactionResult.Status == CommonConstant.TransactionStateMined && transactionResult.Logs
                .Select(l => l.Name).Contains(CommonConstant.VoteEventVoted))
        {
            Log.Information("Ranking vote, transaction success.{0}", transactionId);
            await SaveVotingRecordAsync(chainId, address, votingItemId, RankingVoteStatusEnum.Voted,
                transactionId, category);
            Log.Information("Ranking vote, update app vote.{0}", address);
            var match = Regex.Match(memo ?? string.Empty, CommonConstant.MemoPattern);
            if (match.Success)
            {
                var voteEventLog = transactionResult.Logs.First(l => l.Name == CommonConstant.VoteEventVoted);
                var voteEvent = LogEventDeserializationHelper.DeserializeLogEvent<Voted>(voteEventLog);
                var voteTime = voteEvent.VoteTimestamp.ToDateTime();
                var referral = await _referralInviteProvider.GetByNotVoteInviteeCaHashAsync(chainId, addressCaHash);
                if (referral != null)
                {
                    Log.Information("Ranking vote, referralRelationFirstVote.{0} {1}", address, addressCaHash);
                    referral.FirstVoteTime = voteTime;
                    var inviter = await GetAddressFromCaHash(chainId, referral.InviterCaHash);
                    if (IsValidReferral(referral))
                    {
                        var success = await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(chainId, string.Empty,
                            inviter, UserTask.Daily, UserTaskDetail.DailyFirstInvite, voteTime);
                        var inviteCount = await _referralInviteProvider.IncrementInviteCountAsync(chainId, inviter, 1);
                        Log.Information("RankingVoteInviteCount inviter {inviter} invitee {invitee} inviteCount {inviteCount} success {success}", 
                            inviter, address, inviteCount, success);
                        if (success)
                        {
                            await _rankingAppPointsRedisProvider.IncrementTaskPointsAsync(inviter, UserTaskDetail.DailyFirstInvite);
                            await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, inviter, UserTaskDetail.DailyFirstInvite, voteTime,
                                InformationHelper.GetDailyFirstInviteInformation(address));
                            _logger.LogInformation("generate DailyFirstInvite record success.");
                        }

                        if (inviteCount is > 0 and (5 or 10 or 20))
                        {
                            var userTaskDetail = inviteCount switch
                            {
                                5 => UserTaskDetail.ExploreCumulateFiveInvite,
                                10 => UserTaskDetail.ExploreCumulateTenInvite,
                                20 => UserTaskDetail.ExploreCumulateTwentyInvite,
                            };
                            await _rankingAppPointsRedisProvider.IncrementTaskPointsAsync(inviter, userTaskDetail);
                            await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, inviter, userTaskDetail, voteTime);
                            await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(chainId, string.Empty, inviter,
                                UserTask.Explore, userTaskDetail, voteTime);
                        }
                    }
                    if (IsValidReferralActivity(referral))
                    {
                        referral.IsReferralActivity = true;
                        Log.Information("Ranking vote, referralRelationFirstVoteInActive.{0} {1}", address, inviter);
                        await _rankingAppPointsRedisProvider.IncrementReferralVotePointsAsync(inviter, address, 1);
                        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, inviter, UserTaskDetail.None, PointsType.InviteVote, voteTime, 
                            InformationHelper.GetInviteVoteInformation(address));
                        await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, address, UserTaskDetail.None, PointsType.BeInviteVote, voteTime,
                            InformationHelper.GetBeInviteVoteInformation(inviter));
                    }

                    await _referralInviteProvider.AddOrUpdateAsync(referral);
                }
                
                await ProcessCallBack(address, proposalIndex.ProposalId, trackId, voteTime);
                var alias = match.Groups[1].Value;
                var information = InformationHelper.GetDailyVoteInformation(proposalIndex, alias);
                await _rankingAppPointsRedisProvider.IncrementVotePointsAsync(chainId, votingItemId, address, alias, amount);
                await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, address, UserTaskDetail.DailyVote, voteTime, information);
                _logger.LogInformation("Ranking vote, update app vote success.{0}", address);
                await _messagePublisherService.SendVoteMessageAsync(chainId, votingItemId, address, alias, amount);
                _logger.LogInformation("Ranking vote, send vote message success.{0}", address);
            }
            else
            {
                _logger.LogInformation("Ranking vote, memo mismatch");
            }
        }

        Log.Information("Ranking vote, update transaction status finished.{0}", address);
    }

    private bool IsVoteDuring(ProposalIndex index)
    {
        if (index == null)
        {
            return false;
        }
        return DateTime.UtcNow > index.ActiveStartTime && DateTime.UtcNow < index.ActiveEndTime;
    }

    private async Task<string> GetAddressFromCaHash(string chainId, string caHash)
    {
        if (string.IsNullOrEmpty(caHash))
        {
            return string.Empty;
        }
        var address = await _userAppService.GetUserAddressByCaHashAsync(chainId, caHash);
        if (!string.IsNullOrEmpty(address))
        {
            return address;
        }

        var caHolderInfos = await _portkeyProvider.GetHolderInfosAsync(caHash);
        return caHolderInfos?.CaHolderInfo?.FirstOrDefault(x => x.ChainId == chainId)?.CaHash ?? string.Empty;
    }
    
    private bool IsValidReferral(ReferralInviteRelationIndex referral)
    {
        return !string.IsNullOrEmpty(referral.ReferralCode)
               && !string.IsNullOrEmpty(referral.InviterCaHash);
    }

    private bool IsValidReferralActivity(ReferralInviteRelationIndex referral)
    {
        return IsValidReferral(referral) && _rankingOptions.CurrentValue.ReferralActivityValid;
    }
    
    private async Task<string> CheckAddress(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        return address;
    }
    
    private static RankingType CheckRankingType(string type)
    {
        if (Enum.TryParse<RankingType>(type, true, out var rankingType))
        {
            return rankingType;
        }
        throw new UserFriendlyException($"Invalid rankingType {type}.");
    }

    private async Task ProcessCallBack(string address, string proposalId, string trackId, DateTime voteTime)
    {
        var luckyboxProposalId = _luckyboxOptions.CurrentValue.ProposalId;
        if (luckyboxProposalId == proposalId && !string.IsNullOrEmpty(trackId))
        { 
            await _luckboxTaskProvider.GenerateTaskAsync(address, proposalId, trackId, voteTime);
        }

        var digiStart = _digiOptions.CurrentValue.Start;
        var digiStartTime = _digiOptions.CurrentValue.StartTime;
        if (digiStart)
        {
            await _digiTaskProvider.GenerateTaskAsync(address, digiStartTime, voteTime);
        }
    }
}