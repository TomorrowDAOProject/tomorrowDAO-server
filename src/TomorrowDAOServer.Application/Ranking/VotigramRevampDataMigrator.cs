using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Aetherlink.PriceServer.Common;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Security;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Grains.Grain.Sequence;
using TomorrowDAOServer.Grains.Grain.Votigram;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Ranking;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class VotigramRevampDataMigrator : TomorrowDAOServerAppService, IVotigramRevampDataMigrator
{
    private readonly ILogger<VotigramRevampDataMigrator> _logger;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private readonly IRankingAppProvider _rankingAppProvider;
    private readonly IRankingAppPointsProvider _rankingAppPointsProvider;
    private readonly IRankingAppPointsRedisProvider _rankingAppPointsRedisProvider;
    private readonly IRankingAppPointsCalcProvider _rankingAppPointsCalcProvider;
    private readonly IUserPointsRecordProvider _userPointsRecordProvider;
    private readonly IUserProvider _userProvider;
    private readonly IUserAppService _userAppService;
    private readonly IClusterClient _clusterClient;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;

    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;

    public VotigramRevampDataMigrator(ILogger<VotigramRevampDataMigrator> logger,
        ITelegramAppsProvider telegramAppsProvider, IRankingAppProvider rankingAppProvider,
        IRankingAppPointsProvider rankingAppPointsProvider,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider,
        IUserProvider userProvider,
        IOptionsMonitor<TelegramOptions> telegramOptions, IClusterClient clusterClient,
        IOptionsMonitor<RankingOptions> rankingOptions,
        IRankingAppPointsCalcProvider rankingAppPointsCalcProvider,
        IUserPointsRecordProvider userPointsRecordProvider, IUserAppService userAppService)
    {
        _logger = logger;
        _telegramAppsProvider = telegramAppsProvider;
        _rankingAppProvider = rankingAppProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _telegramOptions = telegramOptions;
        _clusterClient = clusterClient;
        _rankingOptions = rankingOptions;
        _rankingAppPointsCalcProvider = rankingAppPointsCalcProvider;
        _userPointsRecordProvider = userPointsRecordProvider;
        _userAppService = userAppService;
        _userProvider = userProvider;
    }

    public async Task MigrateHistoricalDataAsync(string chainId, bool dealDuplicateApp, bool dealRankingApp,
        bool dealTelegramApp, bool dealRankingAppPointIndex, bool dealRankingAppUserPointsIndex, bool dealUserTaskIndex,
        bool dealUserTotalPoints)
    {
        await CheckAddress(chainId);

        _logger.LogInformation("[VotigramDataMigrate] Start...");

        //1、Deal duplicate
        if (dealDuplicateApp)
        {
            await DealDuplicateAppAsync();
        }

        await Task.Delay(10000);

        //2、Deal Ranking Redis
        if (dealRankingApp)
        {
            await DealRankingAppPointsAsync(chainId);
        }

        await Task.Delay(10000);

        //3、deal app point
        if (dealTelegramApp)
        {
            await DealTelegramAppPointsAsync(chainId);
        }

        //4、deal RankingAppPointIndex
        if (dealRankingAppPointIndex)
        {
            await DealRankingAppPointsIndexAsync(chainId);
        }

        //5、deal RankingUserAppPointsIndex
        if (dealRankingAppUserPointsIndex)
        {
            await DealRankingAppUserPointsIndexAsync(chainId);
        }

        //5、deal user task
        if (dealUserTaskIndex)
        {
            await DealUserPointsIndexAsync(chainId);
        }

        if (dealUserTotalPoints)
        {
            await DealUserTotalPointsAsync(chainId);
        }
    }
    
    [ExceptionHandler(typeof(Exception),  ReturnDefault = ReturnDefault.Default,
        Message = "DealUserTotalPointsAsync fail")]
    private async Task DealUserTotalPointsAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal user total points start...");
        var total = 0;
        try
        {
            var maxResultCount = 100;
            var skipCount = 0;
            List<UserIndex> queryList;
            do
            {
                _logger.LogInformation("[VotigramDataMigrate] Deal user total points. skipCount={0}", skipCount);
                var (totalCount, userList) = await _userAppService.GetUserAsync(
                    new GetUserInput
                    {
                        MaxResultCount = maxResultCount,
                        SkipCount = skipCount,
                        ChainId = chainId
                    });
                queryList = userList;
                _logger.LogInformation("[VotigramDataMigrate] Deal user total points. queryList.Count={0}",
                    queryList.Count);

                foreach (var userIndex in queryList)
                {
                    var addressList = userIndex.AddressInfos?.Select(t => t.Address).Distinct().ToList();
                    if (addressList.IsNullOrEmpty())
                    {
                        _logger.LogWarning("[VotigramDataMigrate] Deal user total points. No Address User，userId={0}",
                            userIndex.UserId);
                        continue;
                    }

                    foreach (var address in addressList)
                    {
                        if (await CheckUserTotalPointsMigrationCompletedAsync(address))
                        {
                            _logger.LogWarning(
                                "[VotigramDataMigrate] User {0} points Migration Completed.",
                                address);
                            continue;
                        }

                        total++;

                        var points = await _rankingAppPointsRedisProvider.GetUserAllPointsByAddressAsync(address);

                        var snapshotDto = new VotigramPointsSnapshotDto
                        {
                            UserTotalPoints = points.ToString(),
                            UserTotalPointsCompleted = true
                        };
                        var newPoints = points / 10;
                        await _rankingAppPointsRedisProvider.SetAsync(
                            RedisHelper.GenerateUserPointsAllCacheKey(address), newPoints.ToString());
                        await SaveRedisDataMigrationCompletedAsync("---", address, snapshotDto);
                    }
                }

                skipCount += queryList.Count;
            } while (!queryList.IsNullOrEmpty() && queryList.Count == maxResultCount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DealUserTotalPointsAsync error.");
        }

        _logger.LogInformation("[VotigramDataMigrate] Deal user total points finished... total={0}", total);
    }

    [ExceptionHandler(typeof(Exception),  ReturnDefault = ReturnDefault.Default,
        Message = "DealUserPointsIndexAsync fail")]
    private async Task DealUserPointsIndexAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal user task index start...");
        var total = 0;
        try
        {
            var maxResultCount = 100;
            var skipCount = 0;
            List<UserPointsIndex> queryList;
            do
            {
                _logger.LogInformation("[VotigramDataMigrate] Deal user task index. skipCount={0}", skipCount);
                var (totalCount, userPointsList) = await _userPointsRecordProvider.GetPointsListAsync(
                    new GetMyPointsInput
                    {
                        MaxResultCount = maxResultCount,
                        SkipCount = skipCount,
                        ChainId = chainId
                    });
                queryList = userPointsList;
                _logger.LogInformation("[VotigramDataMigrate] Deal user task index. queryList.Count={0}",
                    queryList.Count);

                foreach (var userPointsIndex in queryList)
                {
                    var proposalId = userPointsIndex.Id;
                    var alias = string.Empty;
                    if (await CheckUserPointsIndexMigrationCompletedAsync(proposalId, alias))
                    {
                        _logger.LogWarning(
                            "[VotigramDataMigrate] User {0} PointType={1} points Migration Completed.",
                            userPointsIndex.Address, userPointsIndex.PointsType);
                        continue;
                    }

                    total++;

                    var snapshotDto = new VotigramPointsSnapshotDto
                    {
                        UserPointsIndex = JsonConvert.SerializeObject(userPointsIndex),
                        UserPointsIndexCompleted = true
                    };

                    var pointsType = userPointsIndex.PointsType;
                    var points = pointsType is PointsType.Vote or PointsType.BeInviteVote or PointsType.InviteVote
                        ? _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(pointsType, 1)
                        : _rankingAppPointsCalcProvider.CalculatePointsFromPointsType(pointsType);
                    userPointsIndex.Points = points;

                    await SaveRedisDataMigrationCompletedAsync(proposalId, alias, snapshotDto);
                }

                await _userPointsRecordProvider.BulkAddOrUpdateAsync(queryList);

                skipCount += queryList.Count;
            } while (!queryList.IsNullOrEmpty() && queryList.Count == maxResultCount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DealUserPointsIndexAsync error.");
        }

        _logger.LogInformation("[VotigramDataMigrate] Deal user task index finished... totalCount={0}", total);
    }

    [ExceptionHandler(typeof(Exception),  ReturnDefault = ReturnDefault.Default,
        Message = "DealRankingAppUserPointsIndexAsync fail")]
    public virtual async Task DealRankingAppUserPointsIndexAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal ranking user point index start...");
        var voteRecordCount = 0;
        var inviteVoteCount = 0;
        var otherRecordCount = 0;
        try
        {
            var maxResultCount = 100;
            var skipCount = 0;
            List<RankingAppUserPointsIndex> queryList;
            do
            {
                _logger.LogInformation("[VotigramDataMigrate] Deal ranking user point index. skipCount={0}", skipCount);
                var (totalCount, rankingAppPointList) = await _rankingAppPointsProvider.GetRankingAppUserPointsAsync(
                    new GetRankingAppUserPointsInput
                    {
                        MaxResultCount = maxResultCount,
                        SkipCount = skipCount,
                        ChainId = chainId,
                        ExcludePointsTypes = new List<PointsType>() { PointsType.Like }
                    });
                queryList = rankingAppPointList;
                _logger.LogInformation("[VotigramDataMigrate] Deal ranking user point index. queryList.Count={0}",
                    queryList.Count);

                foreach (var rankingAppUserPointsIndex in queryList)
                {
                    var proposalId = rankingAppUserPointsIndex.ProposalId;
                    var alias = rankingAppUserPointsIndex.Alias;
                    if (await CheckRankingAppUserPointsIndexMigrationCompletedAsync(proposalId, alias))
                    {
                        _logger.LogWarning("[VotigramDataMigrate] Ranking {0} App {1} user points  Migration Completed.",
                            proposalId, alias);
                        continue;
                    }

                    var snapshotDto = new VotigramPointsSnapshotDto
                    {
                        RankingAppUserPointsIndex = JsonConvert.SerializeObject(rankingAppUserPointsIndex),
                        RankingAppUserPointsIndexCompleted = true
                    };

                    switch (rankingAppUserPointsIndex.PointsType)
                    {
                        case PointsType.Vote:
                            voteRecordCount++;
                            rankingAppUserPointsIndex.Points =
                                _rankingAppPointsCalcProvider.CalculatePointsFromVotes(rankingAppUserPointsIndex.Amount);
                            break;
                        case PointsType.InviteVote:
                        case PointsType.BeInviteVote:
                            inviteVoteCount++;
                            rankingAppUserPointsIndex.Points =
                                _rankingAppPointsCalcProvider.CalculatePointsFromReferralVotes(rankingAppUserPointsIndex
                                    .Amount);
                            break;
                        default:
                            otherRecordCount++;
                            _logger.LogWarning(
                                "[VotigramDataMigrate] Deal ranking user point index. not supported. pointsType={0}",
                                rankingAppUserPointsIndex.PointsType.ToString());
                            break;
                    }

                    await SaveRedisDataMigrationCompletedAsync(proposalId, alias, snapshotDto);
                    await _rankingAppPointsProvider.AddOrUpdateUserPointsIndexAsync(rankingAppUserPointsIndex);
                }

                skipCount += queryList.Count;
            } while (!queryList.IsNullOrEmpty() && queryList.Count == maxResultCount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DealRankingAppUserPointsIndexAsync error.");
        }

        _logger.LogInformation(
            "[VotigramDataMigrate] Deal ranking user point index finished... voteRecordCount={0}, inviteVoteCount={1},otherRecordCount={2}",
            voteRecordCount, inviteVoteCount, otherRecordCount);
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "DealRankingAppPointsIndexAsync fail")]
    public virtual async Task DealRankingAppPointsIndexAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal ranking point index start...");
        var voteRecordCount = 0;
        var inviteVoteCount = 0;
        var otherRecordCount = 0;
        try
        {
            var maxResultCount = 100;
            var skipCount = 0;
            List<RankingAppPointsIndex> queryList;
            do
            {
                _logger.LogInformation("[VotigramDataMigrate] Deal ranking point index. skipCount={0}", skipCount);
                var (totalCount, rankingAppPointList) = await _rankingAppPointsProvider.GetRankingAppPointsAsync(
                    new GetRankingAppPointsInput
                    {
                        MaxResultCount = maxResultCount,
                        SkipCount = skipCount,
                        ChainId = chainId,
                        ExcludePointsTypes = new List<PointsType>() { PointsType.Like }
                    });
                queryList = rankingAppPointList;
                _logger.LogInformation("[VotigramDataMigrate] Deal ranking point index. queryList.Count={0}",
                    queryList.Count);

                foreach (var rankingAppPointsIndex in queryList)
                {
                    var proposalId = rankingAppPointsIndex.ProposalId;
                    var alias = rankingAppPointsIndex.Alias;
                    if (await CheckRankingAppPointsIndexMigrationCompletedAsync(proposalId, alias))
                    {
                        _logger.LogWarning("[VotigramDataMigrate] Ranking {0} App {1} ES Migration Completed.",
                            proposalId, alias);
                        continue;
                    }

                    var snapshotDto = new VotigramPointsSnapshotDto
                    {
                        RankingAppPointsIndex = JsonConvert.SerializeObject(rankingAppPointsIndex),
                        RankingAppPointsIndexCompleted = true
                    };

                    switch (rankingAppPointsIndex.PointsType)
                    {
                        case PointsType.Vote:
                            voteRecordCount++;
                            rankingAppPointsIndex.Points =
                                _rankingAppPointsCalcProvider.CalculatePointsFromVotes(rankingAppPointsIndex.Amount);
                            break;
                        case PointsType.InviteVote:
                        case PointsType.BeInviteVote:
                            inviteVoteCount++;
                            rankingAppPointsIndex.Points =
                                _rankingAppPointsCalcProvider
                                    .CalculatePointsFromReferralVotes(rankingAppPointsIndex.Amount);
                            break;
                        default:
                            otherRecordCount++;
                            _logger.LogWarning(
                                "[VotigramDataMigrate] Deal ranking point index. not supported. pointsType={0}",
                                rankingAppPointsIndex.PointsType.ToString());
                            break;
                    }

                    await SaveRedisDataMigrationCompletedAsync(proposalId, alias, snapshotDto);
                    await _rankingAppPointsProvider.AddOrUpdateAppPointsIndexAsync(rankingAppPointsIndex);
                }

                skipCount += queryList.Count;
            } while (!queryList.IsNullOrEmpty() && queryList.Count == maxResultCount);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DealRankingAppPointsIndexAsync error.");
        }

        _logger.LogInformation(
            "[VotigramDataMigrate] Deal ranking point index finished...voteRecordCount={0}, inviteVoteCount={1},otherRecordCount={2}",
            voteRecordCount, inviteVoteCount, otherRecordCount);
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "DealTelegramAppPointsAsync fail")]
    public virtual async Task DealTelegramAppPointsAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal telegram app start ...");
        var totalPoint = 0L;
        var totalVote = 0L;
        var totalLike = 0L;
        try
        {
            var telegramAppList = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
            {
                SourceTypes = new List<SourceType>() { SourceType.Telegram, SourceType.FindMini }
            });
            _logger.LogInformation("[VotigramDataMigrate] Telegram App Coun={0}", telegramAppList.Count);
            var appGroups = telegramAppList.Select((app, index) => new { app, index }).GroupBy(x => x.index / 20)
                .Select(g => g.Select(x => x.app).ToList()).ToList();
            var index = -1;
            foreach (var appGroup in appGroups)
            {
                _logger.LogInformation("[VotigramDataMigrate] Deal Group {0}", ++index);
                if (appGroup.IsNullOrEmpty())
                {
                    continue;
                }

                var aliasList = appGroup.Select(t => t.Alias).ToList();
                var rankingAppList = await _rankingAppProvider.GetByAliasAsync(chainId, aliasList);
                var aliasToRankingApp = rankingAppList.GroupBy(t => t.Alias).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var telegramAppIndex in appGroup)
                {
                    var alias = telegramAppIndex.Alias;
                    telegramAppIndex.TotalPoints = 0;
                    telegramAppIndex.TotalVotes = 0;
                    telegramAppIndex.TotalLikes = 0;

                    if (!aliasToRankingApp.ContainsKey(alias))
                    {
                        continue;
                    }

                    var rankingAppIndices = aliasToRankingApp[alias];
                    foreach (var rankingAppIndex in rankingAppIndices)
                    {
                        telegramAppIndex.TotalPoints += rankingAppIndex.TotalPoints;
                        telegramAppIndex.TotalVotes += rankingAppIndex.TotalVotes;
                        telegramAppIndex.TotalLikes += rankingAppIndex.TotalLikes;

                        totalPoint += rankingAppIndex.TotalPoints;
                        totalVote += rankingAppIndex.TotalVotes;
                        totalLike += rankingAppIndex.TotalLikes;
                    }
                }

                await _telegramAppsProvider.BulkAddOrUpdateAsync(appGroup);
                _logger.LogInformation("[VotigramDataMigrate] Telegram App finished. {0}",
                    JsonConvert.SerializeObject(aliasList));
            }

            await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateTotalPointsCacheKey(), totalPoint);
            await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateTotalVotesCacheKey(), totalVote);
            await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateTotalLikesCacheKey(), totalLike);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DealTelegramAppPointsAsync error.");
        }
        _logger.LogInformation("[VotigramDataMigrate] Total Point={0}, Total Vote={1}, Total Like={2}",
            totalPoint, totalVote, totalLike);
    }

    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "DealRankingAppPointsAsync fail")]
    public virtual async Task DealRankingAppPointsAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal ranking app points start...");
        try
        {
            var rankingAppList = await _rankingAppProvider.GetAllRankingAppAsync(chainId);
            _logger.LogInformation("[VotigramDataMigrate] Ranking App count={0}", rankingAppList.Count);
            var proposalIdToRankingApp =
                rankingAppList.GroupBy(t => t.ProposalId).ToDictionary(g => g.Key, g => g.ToList());
            _logger.LogInformation("[VotigramDataMigrate] Ranking App Proposal count={0}", proposalIdToRankingApp.Count);
            foreach (var singleRanking in proposalIdToRankingApp)
            {
                _logger.LogInformation("[VotigramDataMigrate] Deal ranking app points. proposalId={0}...",
                    singleRanking.Key);
                var proposalId = singleRanking.Key;
                var singleRankingAppList = singleRanking.Value;
                var proposalVotePoints = 0L;
                var proposalLikePoints = 0L;
                foreach (var rankingApp in singleRankingAppList)
                {
                    var alias = rankingApp.Alias;
                    if (await CheckRedisDataMigrationCompletedAsync(proposalId, alias))
                    {
                        _logger.LogWarning("[VotigramDataMigrate] Ranking {0} App {1} Migration Completed.",
                            proposalId, alias);
                        continue;
                    }

                    var snapshotDto = new VotigramPointsSnapshotDto();
                    //Deal Redis Vote
                    var (redisKey, votePoints) = await _rankingAppPointsRedisProvider.GetRankingVotePointsAsync(
                        proposalId, alias);
                    snapshotDto.RedisPointSnapshot.Add(new RedisPointSnapshotDto
                    {
                        Key = redisKey,
                        Value = votePoints.ToString()
                    });

                    //deal Redis like
                    var (rankingLikeKey, likePoints) = await _rankingAppPointsRedisProvider.GetRankingLikePointAsync(
                        proposalId, alias);
                    snapshotDto.RedisPointSnapshot.Add(new RedisPointSnapshotDto
                    {
                        Key = rankingLikeKey,
                        Value = likePoints.ToString()
                    });
                    snapshotDto.RedisDataMigrationCompleted = true;

                    var newVotePoints = votePoints / 50;
                    await _rankingAppPointsRedisProvider.SetAsync(redisKey, newVotePoints.ToString());
                    await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateLikedAppCountCacheKey(alias),
                        likePoints);
                    await SaveRedisDataMigrationCompletedAsync(proposalId, alias, snapshotDto);
                    proposalVotePoints += newVotePoints;
                    proposalLikePoints += likePoints;

                    rankingApp.TotalPoints = newVotePoints + likePoints;
                    rankingApp.TotalVotes = newVotePoints / _rankingOptions.CurrentValue.PointsPerVote;
                    rankingApp.TotalLikes = likePoints;
                    _logger.LogInformation("[VotigramDataMigrate] Deal ranking {0} app {1} points finished.",
                        singleRanking.Key, alias);
                }

                await _rankingAppPointsRedisProvider.IncrementAsync(
                    RedisHelper.GenerateProposalVotePointsCacheKey(proposalId),
                    proposalVotePoints);
                await _rankingAppPointsRedisProvider.IncrementAsync(
                    RedisHelper.GenerateProposalLikePointsCacheKey(proposalId),
                    proposalLikePoints);
                await _rankingAppProvider.BulkAddOrUpdateAsync(singleRankingAppList);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DealRankingAppPointsAsync error.");
        }

        _logger.LogInformation("[VotigramDataMigrate] Deal ranking app points end...");
    }

    ////Deal with duplicate app alias
    [ExceptionHandler(typeof(Exception), ReturnDefault = ReturnDefault.Default,
        Message = "DealDuplicateAppAsync fail")]
    public virtual async Task DealDuplicateAppAsync()
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal duplicate apps...");
        try
        {
            var telegramAppList = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
            {
                SourceTypes = new List<SourceType>() { SourceType.Telegram, SourceType.FindMini }
            });
            _logger.LogInformation("[VotigramDataMigrate] Deal duplicate apps count={0}", telegramAppList.Count);
            var appNameToIndex = new Dictionary<string, TelegramAppIndex>();
            var appAlias = new HashSet<string>();
            var deleteIndices = new List<TelegramAppIndex>();
            var updateIndices = new List<TelegramAppIndex>();
            foreach (var telegramAppIndex in telegramAppList)
            {
                if (appNameToIndex.ContainsKey(telegramAppIndex.Title))
                {
                    deleteIndices.Add(telegramAppIndex);
                    continue;
                }

                appNameToIndex[telegramAppIndex.Title] = telegramAppIndex;
                if (appAlias.Contains(telegramAppIndex.Alias))
                {
                    var sequence = await GetSequenceAsync(1);
                    telegramAppIndex.Alias = sequence.First();
                    updateIndices.Add(telegramAppIndex);
                }
                else
                {
                    appAlias.Add(telegramAppIndex.Alias);
                }
            }

            _logger.LogInformation("[VotigramDataMigrate] Delete duplicate app name. {0}",
                JsonConvert.SerializeObject(deleteIndices));
            await _telegramAppsProvider.BulkDeleteAsync(deleteIndices);
            _logger.LogInformation("[VotigramDataMigrate] Update duplicate app alias. {0}",
                JsonConvert.SerializeObject(updateIndices));
            await _telegramAppsProvider.BulkAddOrUpdateAsync(updateIndices);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DealDuplicateAppAsync error.");
        }
        _logger.LogInformation("[VotigramDataMigrate] Deal duplicate apps finished...");
    }

    private async Task CheckAddress(string chainId)
    {
        var address = await _userProvider.GetAndValidateUserAddressAsync(
            CurrentUser.IsAuthenticated ? CurrentUser.GetId() : Guid.Empty, chainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }
    }

    private async Task<List<string>> GetSequenceAsync(int count)
    {
        var sequenceGrain = _clusterClient.GetGrain<ISequenceGrain>(CommonConstant.GrainIdTelegramAppSequence);
        return await sequenceGrain.GetNextValAsync(count);
    }

    private async Task<bool> CheckRedisDataMigrationCompletedAsync(string proposalId, string alias)
    {
        var pointsSnapshotDto = await GetPointSnapshotAsync(proposalId, alias);
        if (pointsSnapshotDto == null)
        {
            return false;
        }

        return pointsSnapshotDto.RedisDataMigrationCompleted;
    }

    private async Task<bool> CheckRankingAppPointsIndexMigrationCompletedAsync(string proposalId, string alias)
    {
        var pointsSnapshotDto = await GetPointSnapshotAsync(proposalId, alias);
        if (pointsSnapshotDto == null)
        {
            return false;
        }

        return pointsSnapshotDto.RankingAppPointsIndexCompleted;
    }

    private async Task<bool> CheckRankingAppUserPointsIndexMigrationCompletedAsync(string proposalId, string alias)
    {
        var pointsSnapshotDto = await GetPointSnapshotAsync(proposalId, alias);
        if (pointsSnapshotDto == null)
        {
            return false;
        }

        return pointsSnapshotDto.RankingAppUserPointsIndexCompleted;
    }

    private async Task<bool> CheckUserPointsIndexMigrationCompletedAsync(string proposalId, string alias)
    {
        var pointsSnapshotDto = await GetPointSnapshotAsync(proposalId, alias);
        if (pointsSnapshotDto == null)
        {
            return false;
        }

        return pointsSnapshotDto.UserPointsIndexCompleted;
    }

    private async Task<bool> CheckUserTotalPointsMigrationCompletedAsync(string address)
    {
        var pointsSnapshotDto = await GetPointSnapshotAsync("---", address);
        if (pointsSnapshotDto == null)
        {
            return false;
        }

        return pointsSnapshotDto.UserTotalPointsCompleted;
    }

    private async Task<VotigramPointsSnapshotDto> GetPointSnapshotAsync(string proposalId, string alias)
    {
        var grain = _clusterClient.GetGrain<IVotigramSnapshotGrain>(IdGeneratorHelper.GenerateId(proposalId, alias));
        var resultDto = await grain.GetPointSnapshotAsync();
        if (resultDto.Success)
        {
            return resultDto.Data;
        }

        return null;
    }

    private async Task SaveRedisDataMigrationCompletedAsync(string proposalId, string alias,
        VotigramPointsSnapshotDto input)
    {
        var grain = _clusterClient.GetGrain<IVotigramSnapshotGrain>(IdGeneratorHelper.GenerateId(proposalId, alias));
        await grain.SavePointSnapshotAsync(input);
    }
}