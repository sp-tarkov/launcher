using SPT.Launcher.ViewModels.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace SPT.Launcher.Views.Notifications
{
    public partial class SPTNotificationView : ReactiveUserControl<SPTNotificationViewModel>
    {
        public SPTNotificationView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
