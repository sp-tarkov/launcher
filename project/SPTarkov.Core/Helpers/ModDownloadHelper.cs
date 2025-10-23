using Microsoft.Extensions.Logging;

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
