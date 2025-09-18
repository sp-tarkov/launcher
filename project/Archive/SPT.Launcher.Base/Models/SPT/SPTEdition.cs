using SPT.Launcher;

namespace SPT.Launcher.Models.SPT
{
    public class SPTEdition
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public SPTEdition(string name) 
        {
            Name = name;
            ServerManager.SelectedServer.profileDescriptions.TryGetValue(name, out string desc);
            Description = desc;
        }
    }
}
