/* EditionCollection.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


using SPT.Launcher.Models.SPT;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher
{
    public class EditionCollection : NotifyPropertyChangedBase
    {
        private bool _hasSelection;
        public bool HasSelection
        {
            get => _hasSelection;
            set => SetProperty(ref _hasSelection, value);
        }
        private int _selectedEditionIndex;
        public int SelectedEditionIndex
        {
            get => _selectedEditionIndex;
            set => SetProperty(ref _selectedEditionIndex, value);
        }

        private SPTEdition _selectedEdition;
        public SPTEdition SelectedEdition
        {
            get => _selectedEdition;
            set => SetProperty(ref _selectedEdition, value, () => HasSelection = _selectedEdition != null);
        }
        public ObservableCollection<SPTEdition> AvailableEditions { get; private set; } = [];

        public EditionCollection()
        {
            SelectedEditionIndex = 0;

            foreach(var edition in ServerManager.SelectedServer.editions)
            {
                AvailableEditions.Add(new SPTEdition(edition));
            }
        }
    }
}
