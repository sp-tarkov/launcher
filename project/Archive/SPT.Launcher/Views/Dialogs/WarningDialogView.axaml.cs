using SPT.Launcher.ViewModels.Dialogs;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace SPT.Launcher.Views.Dialogs
{
    public partial class WarningDialogView : ReactiveUserControl<WarningDialogViewModel>
    {
        public WarningDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
