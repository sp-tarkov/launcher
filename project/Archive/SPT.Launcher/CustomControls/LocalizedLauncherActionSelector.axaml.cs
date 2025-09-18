using SPT.Launcher.Helpers;
using SPT.Launcher.Models;
using SPT.Launcher.Models.Launcher;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace SPT.Launcher.CustomControls
{
    public partial class LocalizedLauncherActionSelector : UserControl
    {
        public LocalizedLauncherActionSelector()
        {
            InitializeComponent();

            this.DetachedFromVisualTree += LocalizedLauncherActionSelector_DetachedFromVisualTree;
            LocalizationProvider.LocaleChanged += LocalizationProvider_LocaleChanged;
        }

        private void LocalizationProvider_LocaleChanged(object? sender, EventArgs e)
        {
            UpdateLocales();
        }

        private void LocalizedLauncherActionSelector_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            //make sure static event handler is released
            LocalizationProvider.LocaleChanged -= LocalizationProvider_LocaleChanged;
        }

        public void UpdateLocales()
        {
            var comboBox = this.FindControl<ComboBox>("combobox");

            foreach (var item in comboBox.Items)
            {
                if(item is LocalizedLauncherAction localizedAction)
                {
                    localizedAction.UpdateLocaleName();
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var comboBox = this.FindControl<ComboBox>("combobox");

            Array actions = Enum.GetValues(typeof(LauncherAction));

            List<LocalizedLauncherAction> actionsList = new List<LocalizedLauncherAction>();

            foreach (var action in actions)
            {
                if (action is LauncherAction launcherAction)
                {
                    actionsList.Add(new LocalizedLauncherAction(launcherAction));
                }
            }

            comboBox.ItemsSource = actionsList;

            foreach(var item in comboBox.Items)
            {
                if(item is LocalizedLauncherAction actionItem && actionItem.Action == LauncherSettingsProvider.Instance.LauncherStartGameAction)
                {
                    comboBox.SelectedItem = item;
                }
            }
        }

        public void combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 1 && e.AddedItems[0] is LocalizedLauncherAction localizedAction)
            {
                LauncherSettingsProvider.Instance.LauncherStartGameAction = localizedAction.Action;
            }
        }
    }
}
