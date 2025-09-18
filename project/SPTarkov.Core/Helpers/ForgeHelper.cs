using SPTarkov.Core.Models;

namespace SPTarkov.Core.Helpers;

public class ForgeHelper
{
    private ConfigHelper _configHelper;
    private HttpHelper _httpHelper;
    private LogHelper _logHelper;

    public ForgeHelper
    (
        LogHelper logHelper,
        ConfigHelper configHelper,
        HttpHelper httpHelper
    )
    {
        _logHelper = logHelper;
        _configHelper = configHelper;
        _httpHelper = httpHelper;
    }

    public async Task<ForgeModsResponse> GetModsFromForge(CancellationToken token, string search = "", string sort = "-featured,name", int page = 1, string? includeFeatured = null)
    {
        return new ForgeModsResponse();
    }
}
