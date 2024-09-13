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
    private List<RankingAppIndex> _defaultRankingAppListCache = new();

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
        IUserAppService userAppService, IPortkeyProvider portkeyProvider)
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

            //var defaultRankingProposal = await GetDefaultRankingProposalAsync(chainId);
            // var defaultRankingProposal = await _proposalProvider.GetDefaultProposalAsync(chainId);
            var defaultRankingProposal = proposalList.MaxBy(p => p.DeployTime);
            if (defaultRankingProposal != null && !defaultRankingProposal.Id.IsNullOrWhiteSpace())
            {
                await GenerateRedisDefaultProposal(defaultRankingProposal.ProposalId, defaultRankingProposal.ProposalDescription, chainId);
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
                    sendTransactionOutput.TransactionId, voteInput.Memo, voteInput.VoteAmount, addressCaHash);

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

    public async Task MoveHistoryDataAsync(string chainId, string type)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        _logger.LogInformation("MoveHistoryDataAsync address {address} chainId {chainId} type {type}", address, chainId, type);
        var historyAppVotes = await _rankingAppProvider.GetNeedMoveRankingAppListAsync();
        var historyUserVotes = (await _voteProvider.GetNeedMoveVoteRecordListAsync())
            .Where(x => x.TotalRecorded == false).ToList();
        switch (type)
        {
            case "1":
                await MoveAppPointsToRedis(historyAppVotes);
                break;
            case "3":
                await UpdateRankingAppInfo(historyAppVotes);
                break;
            case "4":
                await MoveUserPointsToRedis(historyUserVotes);
                break;
            case "5":
                await MoveUserPointsToEs(historyUserVotes);
                break;
            case "6":
                await AddDefaultProposal(chainId);
                break;
            case "7":
                await MoveUserIndexToReferral(chainId);
                break;
        }
    }

    private async Task MoveUserIndexToReferral(string chainId)
    {
        _logger.LogInformation("MoveUserIndexToReferralBegin chainId {count}", chainId);
        var userList = await _userAppService.GetUser();
        var toAdd = userList
            .Where(x => !string.IsNullOrEmpty(x.CaHash))
            .Select(userIndex => new ReferralInviteRelationIndex
            {
                Id = GuidHelper.GenerateId(userIndex.CaHash, CommonConstant.OrganicTrafficBeforeProjectCode, CommonConstant.ProjectCode, CommonConstant.CreateAccountMethodName),
                ChainId = chainId,
                InviterCaHash = string.Empty,
                InviteeCaHash = userIndex.CaHash,
                ReferralCode = CommonConstant.OrganicTrafficBeforeProjectCode,
                ProjectCode = CommonConstant.ProjectCode,
                MethodName = CommonConstant.CreateAccountMethodName,
                Timestamp = userIndex.CreateTime,
                FirstVoteTime = null,
                IsReferralActivity = false
            })
            .ToList();

        await _referralInviteProvider.BulkAddOrUpdateAsync(toAdd);
        _logger.LogInformation("MoveUserIndexToReferralEnd chainId {count}", chainId);
    }

    private async Task AddDefaultProposal(string chainId)
    {
        _logger.LogInformation("AddDefaultProposalBegin chainId {count}", chainId);
        var defaultProposal = await _proposalProvider.GetDefaultProposalAsync(chainId);
        await GenerateRedisDefaultProposal(defaultProposal.ProposalId, defaultProposal.ProposalDescription, chainId);
        _logger.LogInformation("AddDefaultProposalEnd chainId {count}", chainId);
    }

    private async Task MoveAppPointsToRedis(List<RankingAppIndex> historyAppVotes)
    {
        _logger.LogInformation("MoveAppPointsToRedisBegin count {count}", historyAppVotes.Count);
        var appVotesDic = historyAppVotes
            .ToDictionary(x => RedisHelper.GenerateAppPointsVoteCacheKey(x.ProposalId, x.Alias), x => x);
        foreach (var (key, rankingAppIndex) in appVotesDic)
        {
            var voteAmount = (int)rankingAppIndex.VoteAmount;
            var incrementPoints =  _rankingAppPointsCalcProvider.CalculatePointsFromVotes(voteAmount);
            await _rankingAppPointsRedisProvider.IncrementAsync(key, incrementPoints);
        }
        _logger.LogInformation("MoveAppPointsToRedisEnd");
    }
    
    private async Task UpdateRankingAppInfo(List<RankingAppIndex> historyAppVotes)
    {
        //update RankingAppIndex url、LongDescription、Screenshots
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
    
    private async Task MoveUserPointsToRedis(List<VoteRecordIndex> historyUserVotes)
    {
        _logger.LogInformation("MoveUserPointsToRedisBegin count {count}", historyUserVotes.Count);
        var userVotesDic = historyUserVotes
            .GroupBy(record => record.Voter)                
            .ToDictionary(
                group => RedisHelper.GenerateUserPointsAllCacheKey(group.Key), 
                group => group.Sum(record => record.Amount));
        foreach (var (key, voteAmount) in userVotesDic)
        {
            var incrementPoints = _rankingAppPointsCalcProvider.CalculatePointsFromVotes(voteAmount);
            await _rankingAppPointsRedisProvider.IncrementAsync(key, incrementPoints);
        }
        _logger.LogInformation("MoveUserPointsToRedisEnd");
    }

    private async Task MoveUserPointsToEs(List<VoteRecordIndex> historyUserVotes)
    {
        _logger.LogInformation("MoveUserPointsToEsBegin count {count}", historyUserVotes.Count);
        var userProposalAppVotesDic = historyUserVotes
            .GroupBy(x => new { x.ChainId, x.Voter, x.VotingItemId, x.Alias })
            .ToDictionary(
                g => { var key = g.Key; return new { key.ChainId, key.Voter, key.VotingItemId, key.Alias }; },
                g => g.Sum(record => record.Amount)
            );
        foreach (var (key, voteAmount) in userProposalAppVotesDic)
        {
            await _messagePublisherService.SendVoteMessageAsync(key.ChainId, key.VotingItemId, 
                key.Voter, key.Alias, voteAmount);
        }
        _logger.LogInformation("MoveUserPointsToEsEnd");
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
                    //         var daoIndex = await _daoProvider.GetAsync(new GetDAOInfoInput
                    //             { ChainId = chainId, DAOId = daoId });
                    //         var balance =
                    //             await _transferTokenProvider.GetBalanceAsync(chainId, daoIndex!.GovernanceToken, userAddress);
                    //         if (balance.Balance > 0)
                    //         {
                    //             canVoteAmount = 1;
                    //         }
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
        string transactionId, string memo, long amount, string addressCaHash)
    {
        try
        {
            _logger.LogInformation("Ranking vote, update transaction status start.{0}", address);
            var transactionResult = await _contractProvider.QueryTransactionResultAsync(transactionId, chainId);
            var times = 0;
            while ((transactionResult.Status == CommonConstant.TransactionStatePending ||
                    transactionResult.Status == CommonConstant.TransactionStateNotExisted) &&
                   times < _rankingOptions.CurrentValue.RetryTimes)
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
                    var referral = await _referralInviteProvider.GetByNotVoteInviteeCaHashAsync(chainId, addressCaHash);
                    if (referral != null)
                    {
                        _logger.LogInformation("Ranking vote, referralRelationFirstVote.{0}", address);
                        var voteEventLog = transactionResult.Logs.First(l => l.Name == CommonConstant.VoteEventVoted);
                        var voteEvent = LogEventDeserializationHelper.DeserializeLogEvent<Voted>(voteEventLog);
                        referral.FirstVoteTime = voteEvent.VoteTimestamp?.ToDateTime();
                        if (_rankingOptions.CurrentValue.IsReferralActive())
                        {
                            referral.IsReferralActivity = true;
                            var inviter = await GetAddressFromCaHash(chainId, referral.InviterCaHash);
                            _logger.LogInformation("Ranking vote, referralRelationFirstVoteInActive.{0} {1}", address, inviter);
                            await _rankingAppPointsRedisProvider.IncrementReferralVotePointsAsync(inviter, address, 1);
                            await _messagePublisherService.SendReferralFirstVoteMessageAsync(chainId, inviter, address);
                        }
                        else
                        {
                            _logger.LogInformation("Ranking vote, referralRelationFirstVoteNotInActive.{0}", address);
                        }

                        await _referralInviteProvider.AddOrUpdateAsync(referral);
                    }
                    
                    var alias = match.Groups[1].Value;
                    await _rankingAppPointsRedisProvider.IncrementVotePointsAsync(chainId, votingItemId,
                        address, alias, amount);
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
    
    private string GetAliasString(string description)
    {
        return description.Replace(CommonConstant.DescriptionBegin, CommonConstant.EmptyString).Trim();
    }

    private bool IsVoteDuring(ProposalIndex index)
    {
        if (index == null)
        {
            return false;
        }
        return DateTime.UtcNow > index.ActiveStartTime && DateTime.UtcNow < index.ActiveEndTime;
    }

    private async Task GenerateRedisDefaultProposal(string proposalId, string proposalDesc, string chainId)
    {
        var aliasString = GetAliasString(proposalDesc);
        var value = proposalId + CommonConstant.Comma + aliasString;
        // var endTime = proposal.ActiveEndTime;
        await _rankingAppPointsRedisProvider.SaveDefaultRankingProposalIdAsync(chainId, value, null);
    }

    private async Task<string> GetAddressFromCaHash(string chainId, string caHash)
    {
        
        var address = await _userAppService.GetUserAddressByCaHashAsync(chainId, caHash);
        if (!string.IsNullOrEmpty(address))
        {
            return address;
        }

        var caHolderInfos = await _portkeyProvider.GetHolderInfosAsync(caHash);
        return caHolderInfos?.CaHolderInfo?.FirstOrDefault(x => x.ChainId == chainId)?.CaHash ?? string.Empty;

    }
}