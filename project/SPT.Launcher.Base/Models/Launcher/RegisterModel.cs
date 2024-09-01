/* RegisterModel.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using SPT.Launcher.Utilities;
using SPT.Launcher.Models.Launcher;

namespace SPT.Launch.Models.Launcher
{
    public class RegisterModel : NotifyPropertyChangedBase
    {
        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public EditionCollection EditionsCollection { get; set; } = new EditionCollection();
    }
}
