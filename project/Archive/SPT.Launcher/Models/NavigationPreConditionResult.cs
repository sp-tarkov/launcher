using SPT.Launcher.ViewModels;

namespace SPT.Launcher.Models
{
    public class NavigationPreConditionResult
    {
        public bool Succeeded => ErrorMessage == null;
        public string? ErrorMessage { get; private set; } = null;

        public ViewModelBase? ViewModel { get; private set; } = null;

        protected NavigationPreConditionResult(string? ErrorMessage = null, ViewModelBase? OnFailedViewModel = null)
        {
            this.ErrorMessage = ErrorMessage;
            ViewModel = OnFailedViewModel;
        }

        public static NavigationPreConditionResult FromSuccess() => new NavigationPreConditionResult();

        public static NavigationPreConditionResult FromError(string ErrorMessage, ViewModelBase ViewModel) => new NavigationPreConditionResult(ErrorMessage, ViewModel);
    }
}
