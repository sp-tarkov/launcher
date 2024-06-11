using SPT.Launcher.Attributes;
using SPT.Launcher.Controllers;
using SPT.Launcher.Models;
using SPT.Launcher.ViewModels.Notifications;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using ReactiveUI;
using Splat;
using System;
using System.Threading.Tasks;
using DialogHostAvalonia;

namespace SPT.Launcher.ViewModels
{
    public class ViewModelBase : ReactiveObject, IActivatableViewModel, IRoutableViewModel
    {
        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        protected WindowNotificationManager NotificationManager => Locator.Current.GetService<WindowNotificationManager>();

        public string? UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 7);

        public IScreen HostScreen { get; }

        /// <summary>
        /// Delay the return of the viewmodel
        /// </summary>
        /// <param name="Milliseconds">The amount of time in milliseconds to delay</param>
        /// <returns>The viewmodel after the delay time</returns>
        /// <remarks>Useful to delay the navigation to another view. For instance, to allow an animation to complete.</remarks>
        private async Task<ViewModelBase> WithDelay(int Milliseconds)
        {
            await Task.Delay(Milliseconds);

            return this;
        }

        /// <summary>
        /// Tests all preconditions of a viewmodel
        /// </summary>
        /// <param name="ViewModel"></param>
        /// <returns>The first failed precondition or a successful precondition if all tests pass</returns>
        /// <remarks>Execution of preconditions stops at the first failed condition</remarks>
        private NavigationPreConditionResult TestPreConditions(ViewModelBase ViewModel)
        {
            var attribs = ViewModel.GetType().GetCustomAttributes(typeof(NavigationPreCondition), true);

            foreach(var attrib in attribs)
            {
                if(attrib is NavigationPreCondition condition)
                {
                    NavigationPreConditionResult result = condition.TestPreCondition(HostScreen);

                    if(!result.Succeeded)
                    {
                        var vmTypeName = ViewModel.GetType().Name;

                        LogManager.Instance.Warning($"[{vmTypeName}] Failed pre-condition check: {attrib.GetType().Name}");
                        return result;
                    }
                }
            }

            return NavigationPreConditionResult.FromSuccess();
        }

        /// <summary>
        /// Process the results of the precondition tests
        /// </summary>
        /// <param name="ViewModel"></param>
        /// <returns>The viewmodel that should be loaded</returns>
        private ViewModelBase ProcessViewModelResults(ViewModelBase ViewModel)
        {
            NavigationPreConditionResult result = TestPreConditions(ViewModel);

            if (!result.Succeeded)
            {
                ViewModel = result.ViewModel;
            }

            return ViewModel;
        }

        /// <summary>
        /// Navigate to another viewmodel after a delay
        /// </summary>
        /// <param name="ViewModel"></param>
        /// <param name="Milliseconds"></param>
        /// <returns></returns>
        public async Task NavigateToWithDelay(ViewModelBase ViewModel, int Milliseconds)
        {
            ViewModel = ProcessViewModelResults(ViewModel);

            if (ViewModel == null) return;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                HostScreen.Router.Navigate.Execute(await ViewModel.WithDelay(Milliseconds));
            });
        }

        /// <summary>
        /// Navigate to another viewmodel
        /// </summary>
        /// <param name="ViewModel"></param>
        public void NavigateTo(ViewModelBase ViewModel)
        {
            ViewModel = ProcessViewModelResults(ViewModel);

            if (ViewModel == null) return;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                HostScreen.Router.Navigate.Execute(ViewModel);
            });
        }

        /// <summary>
        /// Navigate to the previous viewmodel
        /// </summary>
        public void NavigateBack()
        {
            var ViewModel = HostScreen.Router.NavigationStack[HostScreen.Router.NavigationStack.Count - 2];

            if(ViewModel is ViewModelBase vmBase)
            {
                var result = TestPreConditions(vmBase);

                if (!result.Succeeded)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (result.ViewModel == null) return;

                        HostScreen.Router.Navigate.Execute(result.ViewModel);
                        return;
                    });
                }
            }

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                HostScreen.Router.NavigateBack.Execute();
            });
        }

        /// <summary>
        /// A convenience method for sending notifications
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="Message"></param>
        /// <param name="Type"></param>
        public void SendNotification(string Title, string Message, NotificationType Type = NotificationType.Information)
        {
            NotificationManager.Show(new SPTNotificationViewModel(HostScreen, Title, Message, Type));
        }

        /// <summary>
        /// A convenience method for showing dialogs
        /// </summary>
        /// <param name="ViewModel"></param>
        /// <returns></returns>
        public async Task<object?> ShowDialog(object ViewModel)
        {
            return await DialogHost.Show(ViewModel);
        }

        public ViewModelBase(IScreen Host)
        {
            HostScreen = Host;
        }
    }
}
