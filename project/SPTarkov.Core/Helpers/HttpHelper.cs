using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Web;
using ComponentAce.Compression.Libs.zlib;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge.Responses;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Helpers;

public class HttpHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpHelper> _logger;
    private readonly StateHelper _stateHelper;
    private bool _internetAccess;

    public HttpHelper(
        ILogger<HttpHelper> logger,
        StateHelper stateHelper
    )
    {
        _logger = logger;
        _stateHelper = stateHelper;

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = CertificateValidationCallback;
        handler.UseCookies = false;

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestVersion = new Version(3, 0);
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    private static bool CertificateValidationCallback(
        HttpRequestMessage httpRequestMessage,
        X509Certificate2? x509Certificate2,
        X509Chain? x509Chain,
        SslPolicyErrors sslPolicyErrors
    )
    {
        return true;
    }

    private string BuildGameUrl(string url)
    {
        return "https://" + _stateHelper.SelectedServer?.IpAddress + url;
    }

    public async Task<T?> GameServerGet<T>(string url, CancellationToken token)
    {
        _logger.LogDebug("Get: {Url}", url);

        var task = await _httpClient.GetAsync(BuildGameUrl(url), token);
        var json = SimpleZlib.Decompress(await task.Content.ReadAsByteArrayAsync(token));
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task<T?> GameServerPut<T>(string url, object request, CancellationToken token)
    {
        _logger.LogDebug("Put: {Url}", url);

        var content = new ByteArrayContent(
            SimpleZlib.CompressToBytes(
                JsonSerializer.Serialize(request)
                , zlibConst.Z_BEST_COMPRESSION
            )
        );

        var task = await _httpClient.PutAsync(BuildGameUrl(url), content, token);

        return JsonSerializer.Deserialize<T>(
            SimpleZlib.Decompress(
                await task.Content.ReadAsByteArrayAsync(token)
            )
        );
    }

    public async Task<ForgeModResponse?> ForgeGetMod(string? modId, CancellationToken token)
    {
        var paramsToUse = GetParamsCollection();
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeMod}/{modId}?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetMod Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeModResponse>(response);
    }

    public async Task<ForgeVersionResponse?> ForgeGetModVersion(string modId, string versionId, CancellationToken token)
    {
        var paramsToUse = ParamsCollectionForVersions(versionId);
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeMod}/{modId}/versions?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetModVersion Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeVersionResponse>(response);
    }

    public async Task<ForgeModsResponse?> ForgeGetMods(
        CancellationToken token,
        string search = "",
        string sort = "-featured,name",
        int page = 1,
        string? includeFeatured = null,
        string? includeAi = null
    )
    {
        var paramsToUse = GetParamsCollection(search, sort, ConvertOptionToBool(includeFeatured), ConvertOptionToBool(includeAi));
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeMods}?page={page}&{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetMods Response: {Response}", response);

        if (!task.IsSuccessStatusCode)
        {
            return new ForgeModsResponse
            {
                Success = false
            };
        }

        return JsonSerializer.Deserialize<ForgeModsResponse>(response);
    }

    public async Task<ForgeUpdateResponse?> ForgeGetUpdate(List<string> modGuidsWithVersions, string sptVersion, CancellationToken token)
    {
        var paramsToUse = ParamsCollectionForUpdates(modGuidsWithVersions, sptVersion);
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeUpdate}?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetUpdate Response: {Response}", response);

        if (!task.IsSuccessStatusCode)
        {
            return new ForgeUpdateResponse
            {
                Success = false
            };
        }

        return JsonSerializer.Deserialize<ForgeUpdateResponse>(response);
    }

    public async Task<ForgeAddonResponse?> ForgeGetModAddons(string modId, CancellationToken token)
    {
        var paramsToUse = ParamsCollectionForAddons(modId);
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeAddons}?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetModAddons Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeAddonResponse>(response);
    }

    public async Task<ForgeAddonDetailsResponse?> ForgeGetModAddonDetails(string addonId, CancellationToken token)
    {
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeAddonDetails}/{addonId}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetModAddon Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeAddonDetailsResponse>(response);
    }

    private NameValueCollection GetParamsCollection(string? search = null, string? sort = null, bool? featured = null, bool? ai = null)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("include", "versions,license,category,source_code_links");
        if (!string.IsNullOrWhiteSpace(search))
        {
            queryString.Add("query", search);
        }

        if (featured is not null)
        {
            queryString.Add("filter[featured]", featured.ToString());
        }

        if (ai is not null)
        {
            queryString.Add("filter[contains_ai_content]", ai.ToString());
        }

        queryString.Add("filter[spt_version]", ProgramStatics.SptVersionCompiledFor.ToString());

        if (!string.IsNullOrWhiteSpace(sort))
        {
            queryString.Add("sort", sort);
        }

        return queryString;
    }

    private NameValueCollection ParamsCollectionForVersions(string? versionId = null)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("include", "dependencies,virus_total_links");

        if (!string.IsNullOrEmpty(versionId))
        {
            queryString.Add("filter[id]", versionId);
        }

        return queryString;
    }

    private NameValueCollection ParamsCollectionForUpdates(List<string> modGuidsWithVersions, string sptVersion)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        var strbuilder = new StringBuilder();
        foreach (var modGuidsWithVersion in modGuidsWithVersions)
        {
            strbuilder.Append($"{modGuidsWithVersion},");
        }

        // remove the last ,
        strbuilder.Remove(strbuilder.Length - 1, 1);

        queryString.Add("mods", strbuilder.ToString());
        queryString.Add("spt_version", sptVersion);

        return queryString;
    }

    private NameValueCollection ParamsCollectionForAddons(string modId)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);

        queryString.Add("filter[mod_id]", modId);
        queryString.Add("include", "versions,license,source_code_links");

        return queryString;
    }

    private bool? ConvertOptionToBool(string? selected)
    {
        switch (selected?.ToLower())
        {
            case "exclude":
                return false;
            case "only":
                return true;
            default:
                return null;
        }
    }

    public bool IsInternetAccessAvailable()
    {
        // TODO: change to just pinging forge https://forge.sp-tarkov.com/api/v0/ping?
        try
        {
            using var ping = new Ping();
            var result = ping.Send("8.8.8.8", 1000); // Google's DNS server
            _internetAccess = result.Status == IPStatus.Success;
        }
        catch
        {
            _internetAccess = false;
        }

        _logger.LogDebug("IsInternetAccessAvailable: {InternetAccess}", _internetAccess);

        return _internetAccess;
    }

    private HttpRequestMessage BuildMessage(HttpMethod methodType, string url)
    {
        var message = new HttpRequestMessage(methodType, url);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.UserAgent.Add(new ProductInfoHeaderValue("SinglePlayerTarkovLauncher",
            $"SPT-{ProgramStatics.SptVersionCompiledFor}-{ProgramStatics.SptCommit}"));

        return message;
    }
}
