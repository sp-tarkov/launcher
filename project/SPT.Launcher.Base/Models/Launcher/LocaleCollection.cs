/* LocaleCollection.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using SPT.Launcher.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher
{
    public class LocaleCollection : NotifyPropertyChangedBase
    {
        private string _selectedLocale;
        public string SelectedLocale
        {
            get => _selectedLocale;
            set => SetProperty(ref _selectedLocale, value, () => LocalizationProvider.LoadLocalByName(value));
        }

        public ObservableCollection<string> AvailableLocales { get; set; } = LocalizationProvider.GetAvailableLocales();

        public LocaleCollection()
        {
            SelectedLocale = LocalizationProvider.LocaleNameDictionary.GetValueOrDefault(LauncherSettingsProvider.Instance.DefaultLocale, "English");
        }
    }
}
