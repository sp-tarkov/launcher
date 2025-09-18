/* MenuBarItem.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher
{
    public class MenuBarItem : NotifyPropertyChangedBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private Action _itemAction;
        public Action ItemAction
        {
            get => _itemAction;
            set => SetProperty(ref _itemAction, value);
        }

        public Func<Task<bool>> CanUseAction = async () => await Task.FromResult(true);

        public Action OnFailedToUseAction = null;
    }
}
