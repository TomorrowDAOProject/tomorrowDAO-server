using System.Threading.Tasks;

namespace TomorrowDAOServer.Ranking;

public interface IVotigramRevampDataMigrator
{
    Task MigrateHistoricalDataAsync(string chainId, bool dealDuplicateApp, bool dealRankingApp,
        bool dealTelegramApp, bool dealRankingAppPointIndex, bool dealRankingAppUserPointsIndex, bool dealUserTaskIndex,
        bool dealUserTotalPoints);
}