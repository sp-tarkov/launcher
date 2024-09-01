using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.SPT
{
    public class SPTVersion : NotifyPropertyChangedBase
    {
        public int Major;
        public int Minor;
        public int Build;

        public bool HasTag => Tag != string.Empty;

        private string _tag = string.Empty;
        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value, () => RaisePropertyChanged(nameof(HasTag)));
        }

        public void ParseVersionInfo(string sptVersion)
        {
            if (sptVersion.Contains('-'))
            {
                string[] versionInfo = sptVersion.Split('-');

                sptVersion = versionInfo[0];

                Tag = versionInfo[1];
            }

            string[] splitVersion = sptVersion.Split('.');

            if (splitVersion.Length >= 3)
            {
                int.TryParse(splitVersion[0], out Major);
                int.TryParse(splitVersion[1], out Minor);
                int.TryParse(splitVersion[2], out Build);
            }
        }

        public SPTVersion() { }

        public SPTVersion(string sptVersion)
        {
            ParseVersionInfo(sptVersion);
        }

        public override string ToString()
        {
            return HasTag ? $"{Major}.{Minor}.{Build}-{Tag}" : $"{Major}.{Minor}.{Build}";
        }
    }
}
