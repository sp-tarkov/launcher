/* NotificationItem.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using System;
using System.ComponentModel;
using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher.Notifications
{
    public class NotificationItem : NotifyPropertyChangedBase
    {
        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private string _buttonText;
        public string ButtonText
        {
            get => _buttonText;
            set => SetProperty(ref _buttonText, value);
        }

        private bool _hasButton;
        public bool HasButton
        {
            get => _hasButton;
            set => SetProperty(ref _hasButton, value);
        }

        public Action ItemAction = null;

        public NotificationItem(string message)
        {
            Message = message;
            ButtonText = string.Empty;
            HasButton = false;
        }

        public NotificationItem(string message, string buttonText, Action itemAction)
        {
            Message = message;
            ButtonText = buttonText;
            HasButton = true;
            ItemAction = itemAction;
        }
    }
}
