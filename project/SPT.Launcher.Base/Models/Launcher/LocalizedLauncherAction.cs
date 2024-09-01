using SPT.Launcher.Helpers;
using SPT.Launcher.Models.Launcher;
using System.ComponentModel;
using System.Text.RegularExpressions;
using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models
{
    public class LocalizedLauncherAction : NotifyPropertyChangedBase
    {
        public LauncherAction Action { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public void UpdateLocaleName()
        {
            string value = Action.ToString();

            //this adds an underscore before capitalized letters, except if it is the first letter in the string. Then it is lower cased.
            //The result should be the name of the localization providers property you want to use.
            //Example: MinimizeAction  ->  minimize_action
            string localePropertyName = Regex.Replace(value, "(?<!^)[A-Z]", "_$0").ToLower();

            var locale = LocalizationProvider.Instance.GetType().GetProperty(localePropertyName).GetValue(LocalizationProvider.Instance, null) ?? value;

            if (locale is string localizedName)
            {
                Name = localizedName;
            }
        }

        public LocalizedLauncherAction(LauncherAction action)
        {
            string value = action.ToString();

            Action = action;
            Name = value;

            UpdateLocaleName();
        }
    }
}
