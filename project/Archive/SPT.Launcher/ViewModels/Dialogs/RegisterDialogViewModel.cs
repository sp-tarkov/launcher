using SPT.Launcher.Helpers;
using SPT.Launcher.Models.Launcher;
using ReactiveUI;

namespace SPT.Launcher.ViewModels.Dialogs
{
    public class RegisterDialogViewModel : ViewModelBase
    {
        public string Question { get; set; }
        public string RegisterButtonText { get; set; }
        public string CancelButtonText { get; set; }
        public string ComboBoxPlaceholderText { get; set; }
        public EditionCollection Editions { get; set; } = new EditionCollection();

        /// <summary>
        /// A registration dialog
        /// </summary>
        /// <param name="Host">Set to null when <see cref="ViewModelBase.ShowDialog(object)"/> is used, since the dialog host is handling routing</param>
        /// <param name="Username"></param>
        public RegisterDialogViewModel(IScreen Host, string Username) : base(Host)
        {
            Question = string.Format(LocalizationProvider.Instance.registration_question_format_1, Username);

            RegisterButtonText = LocalizationProvider.Instance.register;

            CancelButtonText = LocalizationProvider.Instance.cancel;

            ComboBoxPlaceholderText = LocalizationProvider.Instance.select_edition;
        }
    }
}
