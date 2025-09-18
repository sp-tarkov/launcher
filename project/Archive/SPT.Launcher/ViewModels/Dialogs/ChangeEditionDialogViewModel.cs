using SPT.Launcher.Models.Launcher;
using ReactiveUI;

namespace SPT.Launcher.ViewModels.Dialogs
{
    public class ChangeEditionDialogViewModel : ViewModelBase
    {
        public EditionCollection editions { get; set; } = new EditionCollection();

        public ChangeEditionDialogViewModel(IScreen Host) : base(Host)
        {
        }
    }
}
