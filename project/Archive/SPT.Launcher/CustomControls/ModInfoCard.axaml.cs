using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SPT.Launcher.CustomControls
{
    public partial class ModInfoCard : UserControl
    {
        public ModInfoCard()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public string Author
        {
            get => GetValue(AuthorProperty);
            set => SetValue(AuthorProperty, value);
        }
        public static readonly StyledProperty<string> AuthorProperty =
            AvaloniaProperty.Register<ModInfoCard, string>(nameof(Author));

        public string ModName
        {
            get => GetValue(ModNameProperty);
            set => SetValue(ModNameProperty, value);
        }
        public static readonly StyledProperty<string> ModNameProperty =
            AvaloniaProperty.Register<ModInfoCard, string>(nameof(ModName));

        public string Version
        {
            get => GetValue(VersionProperty);
            set => SetValue(VersionProperty, value);
        }
        public static readonly StyledProperty<string> VersionProperty =
            AvaloniaProperty.Register<ModInfoCard, string>(nameof(Version));

        public bool IsInServer
        {
            get => GetValue(IsInServerProperty);
            set => SetValue(IsInServerProperty, value);
        }
        public static readonly StyledProperty<bool> IsInServerProperty =
            AvaloniaProperty.Register<ModInfoCard, bool>(nameof(IsInServer));

        public bool IsInProfile
        {
            get => GetValue(IsInProfileProperty);
            set => SetValue(IsInProfileProperty, value);
        }
        public static readonly StyledProperty<bool> IsInProfileProperty =
            AvaloniaProperty.Register<ModInfoCard, bool>(nameof(IsInProfile));

        public string ModUrl
        {
            get => GetValue(ModUrlProperty);
            set => SetValue(ModUrlProperty, value);
        }
        public static readonly StyledProperty<string> ModUrlProperty =
            AvaloniaProperty.Register<ModInfoCard, string>(nameof(ModUrl));

        public ICommand OpenUrlCommand
        {
            get => GetValue(OpenUrlCommandProperty);
            set => SetValue(OpenUrlCommandProperty, value);
        }
        public static readonly StyledProperty<ICommand> OpenUrlCommandProperty =
            AvaloniaProperty.Register<ModInfoCard, ICommand>(nameof(OpenUrlCommand));
    }
}
