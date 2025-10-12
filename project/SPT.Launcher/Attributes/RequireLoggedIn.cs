using ReactiveUI;
using SPT.Launcher.Helpers;
using SPT.Launcher.Models;
using SPT.Launcher.ViewModels;
using System.Threading.Tasks;

namespace SPT.Launcher.Attributes
{
    public class RequireLoggedIn : NavigationPreCondition
    {
        public override async Task<NavigationPreConditionResult> TestPreCondition(IScreen Host)
        {
            AccountStatus status = await AccountManager.LoginAsync(AccountManager.SelectedAccount.username);

            if (status == AccountStatus.OK) return NavigationPreConditionResult.FromSuccess();

            return NavigationPreConditionResult.FromError(LocalizationProvider.Instance.login_failed, new ConnectServerViewModel(Host));
        }
    }
}
