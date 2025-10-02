/* RegisterModel.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using SPT.Launcher.Models.Launcher;
using SPT.Launcher.Utilities;

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

        public EditionCollection EditionsCollection { get; set; } = new EditionCollection();
    }
}
