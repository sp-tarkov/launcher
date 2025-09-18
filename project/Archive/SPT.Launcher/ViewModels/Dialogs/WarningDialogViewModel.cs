using SPT.Launcher.Helpers;
using ReactiveUI;

namespace SPT.Launcher.ViewModels.Dialogs
{
    public class WarningDialogViewModel : ViewModelBase
    {
        public string ButtonText { get; set; }
        public string WarningMessage { get; set; }

        /// <summary>
        /// A warning dialog
        /// </summary>
        /// <param name="Host">Set to null when <see cref="ViewModelBase.ShowDialog(object)"/> is used, since the dialog host is handling routing</param>
        /// <param name="ButtonText"></param>
        /// <param name="WarningMessage"></param>
        public WarningDialogViewModel(IScreen Host, string WarningMessage, string? ButtonText = null) : base(Host)
        {
            this.WarningMessage = WarningMessage;
            this.ButtonText = ButtonText ?? LocalizationProvider.Instance.ok;
        }
    }
}
