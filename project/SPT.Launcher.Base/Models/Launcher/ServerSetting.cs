/* ServerSetting.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher
{
    public class ServerSetting : NotifyPropertyChangedBase
    {
        public LoginModel AutoLoginCreds { get; set; } = null;

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _url;
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }
    }
}
