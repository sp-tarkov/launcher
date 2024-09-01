using SPT.Launcher.Models.SPT;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher
{
    public class ModInfoCollection : NotifyPropertyChangedBase
    {
        private int _serverModsCount;
        public int ServerModsCount
        {
            get => _serverModsCount;
            set => SetProperty(ref _serverModsCount, value);
        }
        
        private int _profileModsCount;
        public int ProfileModsCount
        {
            get => _profileModsCount;
            set => SetProperty(ref _profileModsCount, value);
        }

        private bool _hasMods;
        public bool HasMods
        {
            get => _hasMods;
            set => SetProperty(ref _hasMods, value);
        }

        public ObservableCollection<SPTMod> ActiveMods { get; private set; } = [];
        public ObservableCollection<SPTMod> InactiveMods { get; private set; } = [];

        public ModInfoCollection()
        {
            var serverMods = ServerManager.GetLoadedServerMods().Values.ToList();
            var profileMods = ServerManager.GetProfileMods().ToList();

            ServerModsCount = serverMods?.Count() ?? 0;
            ProfileModsCount = profileMods?.Count() ?? 0;

            foreach (var activeMod in serverMods)
            {
                activeMod.InServer = true;
                ActiveMods.Add(activeMod);
            }

            foreach (var inactiveMod in profileMods)
            {
                var existingMod = ActiveMods.FirstOrDefault(x => x.Name == inactiveMod.Name && x.Version == inactiveMod.Version && x.Author == inactiveMod.Author);

                if (existingMod != null)
                {
                    existingMod.InProfile = true;
                    continue;
                }

                inactiveMod.InProfile = true;
                InactiveMods.Add(inactiveMod);
            }

            HasMods = ActiveMods.Count > 0 || InactiveMods.Count > 0;
        }
    }
}
