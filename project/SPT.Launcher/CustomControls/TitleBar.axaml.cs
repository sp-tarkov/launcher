using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Windows.Input;

namespace SPT.Launcher.CustomControls
{
    public partial class TitleBar : UserControl
    {
        private bool isPointerPressed = false;
        private PixelPoint startPosition = new PixelPoint(0, 0);
        private Point mouseOffsetToOrigin = new Point(0, 0);
        
        public TitleBar()
        {
            InitializeComponent();

            // handle window dragging. Not sure why that stopped working ... -waffle.lazy
            this.PointerPressed += (sender, args) =>
            {
                startPosition = ((Window)this.VisualRoot).Position;
                mouseOffsetToOrigin = args.GetPosition(this);
                isPointerPressed = true;
            };

            this.PointerMoved += (sender, args) =>
            {
                if (isPointerPressed)
                {
                    var pos = args.GetPosition(this);
                    startPosition = new PixelPoint((int)(startPosition.X + pos.X - mouseOffsetToOrigin.X), (int)(startPosition.Y + pos.Y - mouseOffsetToOrigin.Y));
                    ((Window)this.VisualRoot).Position = startPosition;
                }
            };

            this.PointerReleased += (sender, args) =>
            {
                var pos = args.GetPosition(this);
                startPosition = new PixelPoint((int)(startPosition.X + pos.X - mouseOffsetToOrigin.X), (int)(startPosition.Y + pos.Y - mouseOffsetToOrigin.Y));
                ((Window)this.VisualRoot).Position = startPosition;
                isPointerPressed = false;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<TitleBar, string>(nameof(Title));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<IBrush> ButtonForegroundProperty =
            AvaloniaProperty.Register<TitleBar, IBrush>(nameof(ButtonForeground));

        public IBrush ButtonForeground
        {
            get => GetValue(ButtonForegroundProperty);
            set => SetValue(ButtonForegroundProperty, value);
        }

        public static new readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<TitleBar, IBrush>(nameof(Foreground));

        public new IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static new readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<TitleBar, IBrush>(nameof(Background));

        public new IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        //Close Button Command (X Button) Property
        public static readonly StyledProperty<ICommand> XButtonCommandProperty =
            AvaloniaProperty.Register<TitleBar, ICommand>(nameof(XButtonCommand));

        public ICommand XButtonCommand
        {
            get => GetValue(XButtonCommandProperty);
            set => SetValue(XButtonCommandProperty, value);
        }

        //Minimize Button Command (- Button) Property
        public static readonly StyledProperty<ICommand> MinButtonCommandProperty =
            AvaloniaProperty.Register<TitleBar, ICommand>(nameof(MinButtonCommand));

        public ICommand MinButtonCommand
        {
            get => GetValue(MinButtonCommandProperty);
            set => SetValue(MinButtonCommandProperty, value);
        }

        //Setting Button Command Property
        public static readonly StyledProperty<ICommand> SettingsButtonCommandProperty =
            AvaloniaProperty.Register<TitleBar, ICommand>(nameof(SettingsButtonCommand));

        public ICommand SettingsButtonCommand
        {
            get => GetValue(SettingsButtonCommandProperty);
            set => SetValue(SettingsButtonCommandProperty, value);
        }
    }
}
