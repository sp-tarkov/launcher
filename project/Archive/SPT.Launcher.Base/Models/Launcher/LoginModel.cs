/* LoginModel.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher
{
    public class LoginModel : NotifyPropertyChangedBase
    {
        private string _username = "";
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
    }
}
