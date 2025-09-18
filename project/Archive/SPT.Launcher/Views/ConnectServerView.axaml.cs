using SPT.Launcher.ViewModels;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace SPT.Launcher.Views
{
    public partial class ConnectServerView : ReactiveUserControl<ConnectServerViewModel>
    {
        public ConnectServerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}
