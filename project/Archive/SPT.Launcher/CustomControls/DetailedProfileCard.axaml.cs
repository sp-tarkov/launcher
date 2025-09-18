using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using SPT.Launcher.Models.Launcher;

namespace SPT.Launcher.CustomControls;

public partial class DetailedProfileCard : UserControl
{
    public DetailedProfileCard()
    {
        InitializeComponent();

        var border = this.GetControl<Border>("CardBorder");
        border.Opacity = 0;
        border.BoxShadow = BoxShadows.Parse("0 0 0 black");
        border.RenderTransform = TransformOperations.Parse("scale(0.95)");
            
        Task.Run(async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                border.Opacity = 1;
                border.RenderTransform = TransformOperations.Parse("scale(1)");
                border.BoxShadow = BoxShadows.Parse("2 5 5 black");
            });
        });
    }

    public static readonly StyledProperty<ProfileInfo> ProfileInfoProperty =
        AvaloniaProperty.Register<DetailedProfileCard, ProfileInfo>(
            "ProfileInfo");

    public ProfileInfo ProfileInfo
    {
        get => GetValue(ProfileInfoProperty);
        set => SetValue(ProfileInfoProperty, value);
    }

    public static readonly StyledProperty<ICommand> LoginCommandProperty = AvaloniaProperty.Register<DetailedProfileCard, ICommand>(
        "LoginCommand");

    public ICommand LoginCommand
    {
        get => GetValue(LoginCommandProperty);
        set => SetValue(LoginCommandProperty, value);
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (Application.Current != null && Application.Current.TryFindResource("AltBackgroundBrush", this.ActualThemeVariant, out var brush))
        {
            if (brush is ImmutableSolidColorBrush immutableBrush)
            {
                var border = this.GetControl<Border>("CardBorder");

                border.BoxShadow = BoxShadows.Parse("5 7 10 black");
                border.Background = immutableBrush;
                border.RenderTransform = TransformOperations.Parse("scale(1.02)");
            }
        }
    }

    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (Application.Current != null && Application.Current.TryFindResource("BackgroundBrush", this.ActualThemeVariant, out var brush))
        {
            if (brush is ImmutableSolidColorBrush immutableBrush)
            {
                var border = this.GetControl<Border>("CardBorder");

                border.BoxShadow = BoxShadows.Parse("2 5 5 black");
                border.Background = immutableBrush;
                border.RenderTransform = TransformOperations.Parse("scale(1)");
            }
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var border = this.GetControl<Border>("CardBorder");
                
        border.RenderTransform = TransformOperations.Parse("scale(0.98)");
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var border = this.GetControl<Border>("CardBorder");
        var pos = e.GetPosition(border);

        if (border.Bounds.Contains(pos))
        {
            // pointer was released inside the control
            border.RenderTransform = TransformOperations.Parse("scale(1.02)");
            LoginCommand.Execute(ProfileInfo.Username);
            return;
        }
        
        border.RenderTransform = TransformOperations.Parse("scale(1)");        
    }
}