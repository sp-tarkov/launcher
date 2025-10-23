using Microsoft.Extensions.Logging;
using SPTarkov.Core.Logging;

namespace SPTarkov.Core.Helpers;

public class ModDownloadHelper
{
    private ILogger<ModDownloadHelper> _logger;

    public ModDownloadHelper
    (
        ILogger<ModDownloadHelper> logger
    )
    {
        _logger = logger;
    }
}
