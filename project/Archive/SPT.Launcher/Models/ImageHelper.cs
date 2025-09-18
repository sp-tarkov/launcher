using ReactiveUI;

namespace SPT.Launcher.Models
{
    public class ImageHelper : ReactiveObject
    {
        private string _Path;
        public string Path
        {
            get => _Path;
            set => this.RaiseAndSetIfChanged(ref _Path, value);
        }

        /// <summary>
        /// Force property changed by touching the image path.
        /// </summary>
        /// <remarks>Can be used to force image re-loading</remarks>
        public void Touch()
        {
            string tmp = Path;

            Path = "";

            Path = tmp;
        }
    }
}
