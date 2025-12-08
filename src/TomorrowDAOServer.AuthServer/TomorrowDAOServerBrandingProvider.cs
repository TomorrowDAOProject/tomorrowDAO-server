using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace TomorrowDAOServer.Auth;

[Dependency(ReplaceServices = true)]
public class TomorrowDAOServerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "TomorrowDAOServer";
}
