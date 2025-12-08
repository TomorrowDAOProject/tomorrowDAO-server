using TomorrowDAOServer.Localization;
using Volo.Abp.Application.Services;

namespace TomorrowDAOServer;

/* Inherit your application services from this class.
 */
public abstract class TomorrowDAOServerAppService : ApplicationService
{
    protected TomorrowDAOServerAppService()
    {
        LocalizationResource = typeof(TomorrowDAOServerResource);
    }
}
