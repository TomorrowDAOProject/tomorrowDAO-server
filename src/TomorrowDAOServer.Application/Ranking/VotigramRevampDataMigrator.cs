using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using TomorrowDAOServer.Ranking.Provider;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
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
    private readonly IUserProvider _userProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IOptionsMonitor<RankingOptions> _rankingOptions;

    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;

    public VotigramRevampDataMigrator(ILogger<VotigramRevampDataMigrator> logger,
        ITelegramAppsProvider telegramAppsProvider, IRankingAppProvider rankingAppProvider,
        IRankingAppPointsProvider rankingAppPointsProvider,
        IRankingAppPointsRedisProvider rankingAppPointsRedisProvider,
        IUserProvider userProvider,
        IOptionsMonitor<TelegramOptions> telegramOptions, IClusterClient clusterClient,
        IOptionsMonitor<RankingOptions> rankingOptions)
    {
        _logger = logger;
        _telegramAppsProvider = telegramAppsProvider;
        _rankingAppProvider = rankingAppProvider;
        _rankingAppPointsProvider = rankingAppPointsProvider;
        _rankingAppPointsRedisProvider = rankingAppPointsRedisProvider;
        _telegramOptions = telegramOptions;
        _clusterClient = clusterClient;
        _rankingOptions = rankingOptions;
        _userProvider = userProvider;
    }

    public async Task MigrateHistoricalDataAsync(string chainId, bool dealDuplicateApp, bool dealRankingApp, bool dealTelegramApp)
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
    }

    private async Task DealTelegramAppPointsAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal telegram app start ...");
        var telegramAppList = await _telegramAppsProvider.GetAllTelegramAppsAsync(new QueryTelegramAppsInput
        {
            SourceTypes = new List<SourceType>() { SourceType.Telegram, SourceType.FindMini }
        });
        _logger.LogInformation("[VotigramDataMigrate] Telegram App Coun={0}", telegramAppList.Count);
        var appGroups = telegramAppList.Select((app, index) => new { app, index }).GroupBy(x => x.index / 20)
            .Select(g => g.Select(x => x.app).ToList()).ToList();
        var index = -1;
        var totalPoint = 0L;
        var totalVote = 0L;
        var totalLike = 0L;
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
                    totalLike += rankingAppIndex.TotalVotes;
                }
            }

            await _telegramAppsProvider.BulkAddOrUpdateAsync(appGroup);
            _logger.LogInformation("[VotigramDataMigrate] Telegram App finished. {0}",
                JsonConvert.SerializeObject(aliasList));
        }
        await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateTotalPointsCacheKey(), totalPoint);
        await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateTotalVotesCacheKey(), totalVote);
        await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateTotalLikesCacheKey(), totalLike);
        _logger.LogInformation("[VotigramDataMigrate] Total Point={0}, Total Vote={1}, Total Like={2}",
            totalPoint, totalVote, totalLike);
    }

    private async Task DealRankingAppPointsAsync(string chainId)
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal ranking app points start...");
        var rankingAppList = await _rankingAppProvider.GetAllRankingAppAsync(chainId);
        _logger.LogInformation("[VotigramDataMigrate] Ranking App count={0}", rankingAppList.Count);
        var proposalIdToRankingApp =
            rankingAppList.GroupBy(t => t.ProposalId).ToDictionary(g => g.Key, g => g.ToList());
        _logger.LogInformation("[VotigramDataMigrate] Ranking App Proposal count={0}", proposalIdToRankingApp.Count);
        foreach (var singleRanking in proposalIdToRankingApp)
        {
            _logger.LogInformation("[VotigramDataMigrate] Deal ranking app points. proposalId={0}...", singleRanking.Key);
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

                var newVotePoints = votePoints / 10;
                await _rankingAppPointsRedisProvider.SetAsync(redisKey, newVotePoints.ToString());
                await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateLikedAppCountCacheKey(alias),
                    likePoints);
                await SaveRedisDataMigrationCompletedAsync(proposalId, alias, snapshotDto);
                proposalVotePoints += newVotePoints;
                proposalLikePoints += likePoints;

                rankingApp.TotalPoints = newVotePoints;
                rankingApp.TotalVotes = newVotePoints / _rankingOptions.CurrentValue.PointsPerVote;
                rankingApp.TotalLikes = likePoints;
                _logger.LogInformation("[VotigramDataMigrate] Deal ranking {0} app {1} points finished.", singleRanking.Key, alias);
            }

            await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateProposalVotePointsCacheKey(proposalId),
                proposalVotePoints);
            await _rankingAppPointsRedisProvider.IncrementAsync(RedisHelper.GenerateProposalLikePointsCacheKey(proposalId),
                proposalLikePoints);
            await _rankingAppProvider.BulkAddOrUpdateAsync(singleRankingAppList);
        }
        _logger.LogInformation("[VotigramDataMigrate] Deal ranking app points end...");
    }

    ////Deal with duplicate app alias
    private async Task DealDuplicateAppAsync()
    {
        _logger.LogInformation("[VotigramDataMigrate] Deal duplicate apps...");
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

    private async Task<bool> CheckAppDataMigrationCompletedAsync(string proposalId, string alias)
    {
        var pointsSnapshotDto = await GetPointSnapshotAsync(proposalId, alias);
        if (pointsSnapshotDto == null)
        {
            return false;
        }

        return pointsSnapshotDto.AppDataMigrationCompleted;
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

    private async Task SaveRedisDataMigrationCompletedAsync(string proposalId, string alias, VotigramPointsSnapshotDto input)
    {
        var grain = _clusterClient.GetGrain<IVotigramSnapshotGrain>(IdGeneratorHelper.GenerateId(proposalId, alias));
        await grain.SavePointSnapshotAsync(input);
    }
}