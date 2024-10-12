using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using TomorrowDAO.Contracts.Vote;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.AElfSdk;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.MQ;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Proposal.Provider;
using TomorrowDAOServer.Providers;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Referral.Dto;
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
        IUserPointsRecordProvider userPointsRecordProvider)
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
        _voteProvider = voteProvider;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
    }

    public async Task GenerateRankingApp(string chainId, List<IndexerProposal> proposalList)
    {
        var toUpdate = new List<RankingAppIndex>();
        foreach (var proposal in proposalList)
        {
            var aliases = GetAliasList(proposal.ProposalDescription);
            var telegramApps = (await _telegramAppsProvider.GetTelegramAppsAsync(new QueryTelegramAppsInput
            {
                Aliases = aliases
            })).Item2;
            var rankingApps = _objectMapper.Map<List<TelegramAppIndex>, List<RankingAppIndex>>(telegramApps);
            foreach (var rankingApp in rankingApps)
            {
                _objectMapper.Map(proposal, rankingApp);
                rankingApp.Id =
                    GuidHelper.GenerateGrainId(proposal.ChainId, proposal.DAOId, proposal.Id, rankingApp.AppId);
            }

            toUpdate.AddRange(rankingApps);
        }

        if (!toUpdate.IsNullOrEmpty())
        {
            await _rankingAppProvider.BulkAddOrUpdateAsync(toUpdate);
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

    public async Task<PageResultDto<RankingListDto>> GetRankingProposalListAsync(GetRankingListInput input)
    {
        var result = await _proposalProvider.GetRankingProposalListAsync(input);
        return new PageResultDto<RankingListDto>
        {
            TotalCount = result.Item1,
            Data = ObjectMapper.Map<List<ProposalIndex>, List<RankingListDto>>(result.Item2)
        };
    }

    public async Task<RankingVoteResponse> VoteAsync(RankingVoteInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.RawTransaction.IsNullOrWhiteSpace())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        _logger.LogInformation("Ranking vote, start...");
        var (address, addressCaHash) =
            await _userProvider.GetAndValidateUserAddressAndCaHashAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input!.ChainId);
        if (address.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("User Address Not Found.");
        }

        _logger.LogInformation("Ranking vote, parse rawTransaction. {0}", address);
        var (voteInput, transaction) = ParseRawTransaction(input.ChainId, input.RawTransaction);
        var votingItemId = voteInput.VotingItemId.ToHex();
        var proposalIndex = await _proposalProvider.GetProposalByIdAsync(input.ChainId, votingItemId);
        if (!IsVoteDuring(proposalIndex))
        {
            throw new UserFriendlyException("Can not vote now.");
        }

        _logger.LogInformation("Ranking vote, query voting record.{0}", address);
        var votingRecord = await GetRankingVoteRecordAsync(input.ChainId, address, votingItemId);
        if (votingRecord != null)
        {
            _logger.LogInformation("Ranking vote, vote exist. {0}", address);
            return BuildRankingVoteResponse(votingRecord.Status, votingRecord.TransactionId);
        }

        IAbpDistributedLockHandle lockHandle = null;
        try
        {
            _logger.LogInformation("Ranking vote, lock. {0}", address);
            var distributedLockKey =
                RedisHelper.GenerateDistributedLockKey(input.ChainId, address, voteInput.VotingItemId?.ToHex());
            lockHandle = await _distributedLock.TryAcquireAsync(distributedLockKey,
                _rankingOptions.CurrentValue.GetLockUserTimeoutTimeSpan());
            {
                if (lockHandle == null)
                {
                    _logger.LogInformation("Ranking vote, lock failed. {0}", address);
                    return BuildRankingVoteResponse(RankingVoteStatusEnum.Failed);
                }

                _logger.LogInformation("Ranking vote, query voting record again.{0}", address);
                votingRecord = await GetRankingVoteRecordAsync(input.ChainId, address, votingItemId);
                if (votingRecord != null)
                {
                    _logger.LogInformation("Ranking vote, vote exist. {0}", address);
                    return BuildRankingVoteResponse(votingRecord.Status, votingRecord.TransactionId);
                }

                _logger.LogInformation("Ranking vote, send transaction. {0}", address);
                var sendTransactionOutput = await _contractProvider.SendTransactionAsync(input.ChainId, transaction);
                if (sendTransactionOutput.TransactionId.IsNullOrWhiteSpace())
                {
                    _logger.LogError("Ranking vote, send transaction error, {0}",
                        JsonConvert.SerializeObject(sendTransactionOutput));
                    return BuildRankingVoteResponse(RankingVoteStatusEnum.Failed);
                }

                _logger.LogInformation("Ranking vote, send transaction success. {0}", address);
                await SaveVotingRecordAsync(input.ChainId, address, votingItemId, RankingVoteStatusEnum.Voting,
                    sendTransactionOutput.TransactionId, _rankingOptions.CurrentValue.GetVoteTimoutTimeSpan());

                var _ = UpdateVotingStatusAsync(input.ChainId, address, votingItemId,
                    sendTransactionOutput.TransactionId, voteInput.Memo, voteInput.VoteAmount, addressCaHash,
                    proposalIndex);

                return BuildRankingVoteResponse(RankingVoteStatusEnum.Voting, sendTransactionOutput.TransactionId);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ranking vote, error. {0}", JsonConvert.SerializeObject(input));
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

        var voteRecord = await GetRankingVoteRecordAsync(input!.ChainId, input.Address, input.ProposalId);
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
            var points = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(input.Address);
            voteRecord.TotalPoints = points;
        }

        return voteRecord;
    }

    public async Task MoveHistoryDataAsync(string chainId, string type, string key, string value)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        _logger.LogInformation("MoveHistoryDataAsync address {address} chainId {chainId} type {type}", address, chainId,
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
            case "9":
                searchValue = await _rankingAppPointsRedisProvider.GetAsync(key);
                _logger.LogInformation("RedisValue key {key} value {value}", key, searchValue);
                break;
            case "10":
                searchValue = await _distributedCache.GetAsync(key);
                _logger.LogInformation("RedisDistributedCacheValue key {key} value {value}", key, searchValue);
                break;
            case "11":
                await _rankingAppPointsRedisProvider.SetAsync(key, value);
                break;
        }
    }

    private async Task VoteRecordToPointsRecord(string chainId)
    {
        _logger.LogInformation("VoteRecordToPointsRecordBegin chainId {chainId}", chainId);
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
        _logger.LogInformation("VoteRecordToPointsRecordEnd chainId {chainId} count {count}", chainId, toAdd.Count);
    }

    private async Task ReferralInviteToPointsRecord(string chainId)
    {
        _logger.LogInformation("ReferralInviteToPointsRecordBegin chainId {chainId}", chainId);
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
        _logger.LogInformation("ReferralInviteToPointsRecordEnd chainId {chainId} voteRecordIsNull {voteRecordIsNull}", 
            chainId, voteRecord == null);
    }
    
    private async Task GetReferralInviteCountToGrain(string chainId)
    {
        _logger.LogInformation("GetReferralInviteCountToGrainBegin chainId {chainId}", chainId);
        var inviter = _rankingOptions.CurrentValue.ReferralPointsAddressList[0];
        var count = await _referralInviteProvider.GetInviteCountAsync(chainId, inviter);
        _logger.LogInformation("GetReferralInviteCountToGrainEnd chainId {chainId} count {count}", chainId, count);
    }

    private async Task ReferralInviteCountToGrain(string chainId)
    {
        _logger.LogInformation("ReferralInviteCountToGrainBegin chainId {chainId}", chainId);
        var inviter = _rankingOptions.CurrentValue.ReferralPointsAddressList[0];
        await _referralInviteProvider.IncrementInviteCountAsync(chainId, inviter, 1);
        _logger.LogInformation("ReferralInviteCountToGrainEnd chainId {chainId}", chainId);
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

    public async Task<long> LikeAsync(RankingAppLikeInput input)
    {
        if (input == null || input.ChainId.IsNullOrWhiteSpace() || input.ProposalId.IsNullOrWhiteSpace() ||
            input.LikeList.IsNullOrEmpty())
        {
            ExceptionHelper.ThrowArgumentException();
        }

        var address =
            await _userProvider.GetAndValidateUserAddressAsync(
                CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, input.ChainId);
        if (address.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("User Address Not Found.");
        }

        try
        {
            var defaultProposalId = await _rankingAppPointsRedisProvider.GetDefaultRankingProposalIdAsync(input.ChainId);
            if (input.ProposalId != defaultProposalId)
            {
                throw new UserFriendlyException($"Cannot be liked.{defaultProposalId}");
            }
            
            await _rankingAppPointsRedisProvider.IncrementLikePointsAsync(input, address);
            
            var _ = _messagePublisherService.SendLikeMessageAsync(input.ChainId, input.ProposalId, address, input.LikeList);

            return await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(address);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ranking like, error. {0}", JsonConvert.SerializeObject(input));
            ExceptionHelper.ThrowSystemException("liking", e);
            return 0;
        }
    }

    private async Task SaveVotingRecordAsync(string chainId, string address,
        string proposalId, RankingVoteStatusEnum status, string transactionId, TimeSpan? expire = null)
    {
        var distributeCacheKey = RedisHelper.GenerateDistributeCacheKey(chainId, address, proposalId);
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

    public async Task<RankingDetailDto> GetRankingProposalDetailAsync(string userAddress, string chainId,
        string proposalId)
    {
        var userAllPoints = await _rankingAppPointsRedisProvider.GetUserAllPointsAsync(userAddress);
        if (proposalId.IsNullOrEmpty())
        {
            return new RankingDetailDto { UserTotalPoints = userAllPoints };
        }

        var rankingAppList = await _rankingAppProvider.GetByProposalIdAsync(chainId, proposalId);
        if (rankingAppList.IsNullOrEmpty())
        {
            return new RankingDetailDto { UserTotalPoints = userAllPoints };
        }

        var rankingApp = rankingAppList[0];
        var canVoteAmount = 0;
        var proposalDescription = rankingApp.ProposalDescription;
        if (DateTime.UtcNow < rankingApp.ActiveEndTime)
        {
            var voteRecordRedis = await GetRankingVoteRecordAsync(chainId, userAddress, proposalId);
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
        var aliasList = GetAliasList(proposalDescription);
        var appPointsList = await _rankingAppPointsRedisProvider.GetAllAppPointsAsync(chainId, proposalId, aliasList);
        var appVoteAmountDic = appPointsList
            .Where(x => x.PointsType == PointsType.Vote)
            .ToDictionary(x => x.Alias, x => _rankingAppPointsCalcProvider.CalculateVotesFromPoints(x.Points));
        var totalVoteAmount = appVoteAmountDic.Values.Sum();
        var totalPoints = appPointsList.Sum(x => x.Points);
        var votePercentFactor = DoubleHelper.GetFactor(totalVoteAmount);
        var pointsPercentFactor = DoubleHelper.GetFactor(totalPoints);
        var appPointsDic = RankingAppPointsDto
            .ConvertToBaseList(appPointsList)
            .ToDictionary(x => x.Alias, x => x.Points);
        var rankingList = ObjectMapper.Map<List<RankingAppIndex>, List<RankingAppDetailDto>>(rankingAppList);
        foreach (var app in rankingList)
        {
            app.PointsAmount = appPointsDic.GetValueOrDefault(app.Alias, 0);
            app.VoteAmount = appVoteAmountDic.GetValueOrDefault(app.Alias, 0);
            app.VotePercent = appVoteAmountDic.GetValueOrDefault(app.Alias, 0) * votePercentFactor;
            app.PointsPercent = app.PointsAmount * pointsPercentFactor;
        }
        
        return new RankingDetailDto
        {
            StartTime = rankingApp.ActiveStartTime,
            EndTime = rankingApp.ActiveEndTime,
            CanVoteAmount = canVoteAmount,
            TotalVoteAmount = totalVoteAmount,
            UserTotalPoints = userAllPoints,
            RankingList = rankingList.OrderByDescending(r => r.PointsAmount)
                .ThenBy(r => aliasList.IndexOf(r.Alias)).ToList()
        };
    }

    private Tuple<VoteInput, Transaction> ParseRawTransaction(string chainId, string rawTransaction)
    {
        try
        {
            var bytes = ByteArrayHelper.HexStringToByteArray(rawTransaction);
            var transaction = Transaction.Parser.ParseFrom(bytes);

            VoteInput voteInput = null;
            var caAddress = _contractProvider.ContractAddress(chainId, CommonConstant.CaContractAddressName);
            var voteAddress = _contractProvider.ContractAddress(chainId, CommonConstant.VoteContractAddressName);
            if (transaction.To.ToBase58() == caAddress && transaction.MethodName == "ManagerForwardCall")
            {
                var managerForwardCallInput = ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
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
        catch (Exception e)
        {
            _logger.LogError(e, "VoteAsync error. {0}", rawTransaction);
            ExceptionHelper.ThrowArgumentException();
            return new Tuple<VoteInput, Transaction>(new VoteInput(), new Transaction());
        }
    }

    private RankingVoteResponse BuildRankingVoteResponse(RankingVoteStatusEnum status, string TranscationId = null)
    {
        return new RankingVoteResponse
        {
            Status = status,
            TransactionId = TranscationId
        };
    }

    public async Task<RankingVoteRecord> GetRankingVoteRecordAsync(string chainId, string address, string proposalId)
    {
        var distributeCacheKey = RedisHelper.GenerateDistributeCacheKey(chainId, address, proposalId);
        var cache = await _distributedCache.GetAsync(distributeCacheKey);
        return cache.IsNullOrWhiteSpace() ? null : JsonConvert.DeserializeObject<RankingVoteRecord>(cache);
    }

    public async Task<VoteRecordIndex> GetRankingVoteRecordEsAsync(string chainId, string address, string proposalId)
    {
        try
        {
            return (await _voteProvider.GetByVoterAndVotingItemIdsAsync(chainId, address,
                    new List<string> { proposalId }))
                .Where(x => x.VoteTime.ToString(CommonConstant.DayFormatString) ==
                            DateTime.UtcNow.ToString(CommonConstant.DayFormatString))
                .ToList().SingleOrDefault();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetRankingVoteRecordEsAsyncException");
            return null;
        }
    }

    private TimeSpan GetCacheExpireTimeSpan()
    {
        var nowUtc = DateTime.UtcNow;
        var nextDay = nowUtc.Date.AddDays(1);
        return nextDay - nowUtc;
    }

    private async Task UpdateVotingStatusAsync(string chainId, string address, string votingItemId,
        string transactionId, string memo, long amount, string addressCaHash, ProposalIndex proposalIndex)
    {
        try
        {
            _logger.LogInformation("Ranking vote, update transaction status start.{0}", address);
            var transactionResult = await _contractProvider.QueryTransactionResultAsync(transactionId, chainId);
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
                _logger.LogInformation("Ranking vote, transaction success.{0}", transactionId);
                await SaveVotingRecordAsync(chainId, address, votingItemId, RankingVoteStatusEnum.Voted,
                    transactionId);
                _logger.LogInformation("Ranking vote, update app vote.{0}", address);
                var match = Regex.Match(memo ?? string.Empty, CommonConstant.MemoPattern);
                if (match.Success)
                {
                    var voteEventLog = transactionResult.Logs.First(l => l.Name == CommonConstant.VoteEventVoted);
                    var voteEvent = LogEventDeserializationHelper.DeserializeLogEvent<Voted>(voteEventLog);
                    var voteTime = voteEvent.VoteTimestamp.ToDateTime();
                    var referral = await _referralInviteProvider.GetByNotVoteInviteeCaHashAsync(chainId, addressCaHash);
                    if (referral != null)
                    {
                        _logger.LogInformation("Ranking vote, referralRelationFirstVote.{0} {1}", address, addressCaHash);
                        referral.FirstVoteTime = voteTime;
                        var inviter = await GetAddressFromCaHash(chainId, referral.InviterCaHash);
                        if (IsValidReferral(referral))
                        {
                            var success = await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(chainId, inviter, UserTask.Daily,
                                UserTaskDetail.DailyFirstInvite, voteTime);
                            var inviteCount = await _referralInviteProvider.IncrementInviteCountAsync(chainId, inviter, 1);
                            _logger.LogInformation("RankingVoteInviteCount inviter {inviter} invitee {invitee} inviteCount {inviteCount} success {success}", 
                                inviter, address, inviteCount, success);
                            if (success)
                            {
                                await _rankingAppPointsRedisProvider.IncrementTaskPointsAsync(inviter, UserTaskDetail.DailyFirstInvite);
                                await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, inviter, UserTaskDetail.DailyFirstInvite, voteTime,
                                    InformationHelper.GetDailyFirstInviteInformation(address));
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
                                await _userPointsRecordProvider.UpdateUserTaskCompleteTimeAsync(chainId, inviter, UserTask.Explore,
                                    userTaskDetail, voteTime);
                            }
                        }
                        if (IsValidReferralActivity(referral, voteTime))
                        {
                            referral.IsReferralActivity = true;
                            _logger.LogInformation("Ranking vote, referralRelationFirstVoteInActive.{0} {1}", address, inviter);
                            await _rankingAppPointsRedisProvider.IncrementReferralVotePointsAsync(inviter, address, 1);
                            await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, inviter, UserTaskDetail.None, PointsType.InviteVote, voteTime, 
                                InformationHelper.GetInviteVoteInformation(address));
                            await _userPointsRecordProvider.GenerateTaskPointsRecordAsync(chainId, address, UserTaskDetail.None, PointsType.BeInviteVote, voteTime,
                                InformationHelper.GetBeInviteVoteInformation(inviter));
                        }

                        await _referralInviteProvider.AddOrUpdateAsync(referral);
                    }
                    
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

            _logger.LogInformation("Ranking vote, update transaction status finished.{0}", address);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ranking vote, update transaction status error.{0}", transactionId);
        }
    }

    private List<string> GetAliasList(string description)
    {
        return description.Replace(CommonConstant.DescriptionBegin, CommonConstant.EmptyString)
            .Trim().Split(CommonConstant.Comma).Select(alias => alias.Trim()).Distinct().ToList();
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

    private bool IsValidReferralActivity(ReferralInviteRelationIndex referral, DateTime voteTime)
    {
        return IsValidReferral(referral) && _rankingOptions.CurrentValue.IsReferralActive(voteTime);
    }
}