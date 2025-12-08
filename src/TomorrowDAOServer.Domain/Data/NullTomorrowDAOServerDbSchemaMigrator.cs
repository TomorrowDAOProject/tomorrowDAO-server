using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Data;

/* This is used if database provider does't define
 * ITomorrowDAOServerDbSchemaMigrator implementation.
 */
public class NullTomorrowDAOServerDbSchemaMigrator : ITomorrowDAOServerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
