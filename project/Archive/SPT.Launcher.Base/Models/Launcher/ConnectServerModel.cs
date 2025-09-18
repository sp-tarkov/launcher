/* ConnectServerModel.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher
{
    public class ConnectServerModel : NotifyPropertyChangedBase
    {
        private string _infoText;
        public string InfoText
        {
            get => _infoText;
            set => SetProperty(ref _infoText, value);
        }

        private bool _connectionFailed;
        public bool ConnectionFailed
        {
            get => _connectionFailed;
            set => SetProperty(ref _connectionFailed, value);
        }
    }
}
