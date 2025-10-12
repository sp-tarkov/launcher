using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SPT.Launcher.Controllers;
using SPT.Launcher.Helpers;
using SPT.Launcher.Models.Launcher;
using SPT.Launcher.ViewModels.Dialogs;
using ReactiveUI;

namespace SPT.Launcher.ViewModels;

public class ModInfoViewModel(IScreen Host, ModInfoCollection mods) : ViewModelBase(Host)
{
    public ModInfoCollection ModsCollection { get; set; } = mods;

    public async Task OpenUrlCommand(string url)
    {
        if (!url.StartsWith("https://"))
        {
            LogManager.Instance.Warning($"url does not start with http/s \n  -URL-> '{url}'");
            return;
        }

        var question = string.Format(LocalizationProvider.Instance.open_link_question_format_1, url);
        var confirmText = LocalizationProvider.Instance.open_link;
        var cancelText = LocalizationProvider.Instance.cancel;

        var confirm = await ShowDialog(new ConfirmationDialogViewModel(HostScreen, question, confirmText, cancelText));

        if (confirm is not (bool and true))
            return;
            
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            ArgumentList = { "/C", "start", url }
        });
    }
}