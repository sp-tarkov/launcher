using SPT.Launcher.Helpers;
using SPT.Launcher.Models;
using SPT.Launcher.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;

namespace SPT.Launcher.Attributes
{
    public class RequireServerConnected : NavigationPreCondition
    {
        public override async Task<NavigationPreConditionResult> TestPreCondition(IScreen Host)
        {
            if (await ServerManager.PingServerAsync()) return NavigationPreConditionResult.FromSuccess();

            string error = string.Format(LocalizationProvider.Instance.server_unavailable_format_1, LauncherSettingsProvider.Instance.Server.Name);

            return NavigationPreConditionResult.FromError(error, new ConnectServerViewModel(Host));
        }
    }
}
