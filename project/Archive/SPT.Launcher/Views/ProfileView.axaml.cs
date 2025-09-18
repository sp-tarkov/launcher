using SPT.Launcher.ViewModels;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace SPT.Launcher.Views
{
    public partial class ProfileView : ReactiveUserControl<ProfileViewModel>
    {
        public ProfileView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
