using System.Threading.Tasks;

namespace TomorrowDAOServer.Data;

public interface ITomorrowDAOServerDbSchemaMigrator
{
    Task MigrateAsync();
}
