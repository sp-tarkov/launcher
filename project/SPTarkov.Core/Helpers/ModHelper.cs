using Microsoft.Extensions.Logging;
using SPTarkov.Core.Logging;

namespace SPTarkov.Core.Helpers;

public class ModHelper
{
    private readonly string _backupClientModPath = "D:\\Repos\\CWX-SPT-Launcher\\emulatedMods\\BackupClientMods"; // this is temp
    private readonly string _backupServerModPath = "D:\\Repos\\CWX-SPT-Launcher\\emulatedMods\\BackupServerMods"; // this is temp
    private readonly string _clientModPath = "D:\\Repos\\CWX-SPT-Launcher\\emulatedMods\\ClientMods"; // this is temp
    private readonly string _serverModPath = "D:\\Repos\\CWX-SPT-Launcher\\emulatedMods\\ServerMods"; // this is temp
    private readonly List<string> _ServerModRootDirectories = [];
    private List<string> _clientModRootDirectories = [];
    private ConfigHelper _configHelper;
    private ILogger<ModHelper> _logger;

    public ModHelper
    (
        ILogger<ModHelper> logger,
        ConfigHelper configHelper
    )
    {
        _logger = logger;
        _configHelper = configHelper;
    }

    public async Task GetServerMods()
    {
        var serverModPackageJsons = Directory.GetFiles(_serverModPath, "package.json", SearchOption.AllDirectories).ToList();

        foreach (var serverModPackageJson in serverModPackageJsons)
        {
            _ServerModRootDirectories.Add(serverModPackageJson.Replace("/package.json", ""));
        }
    }

    public async Task GetClientMods()
    {
        var clientModDlls = Directory.GetFiles(_clientModPath, "*.dll", SearchOption.AllDirectories).ToList();

        foreach (var dll in clientModDlls)
        {
            // find out if the Dll can be loaded by bepinex by checking for a method with attribute
            // if it cant disregard it
            // else add to _clientModRootDirectories
        }
    }
}
