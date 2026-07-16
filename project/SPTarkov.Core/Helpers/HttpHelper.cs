using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Web;
using ComponentAce.Compression.Libs.zlib;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Forge;
using SPTarkov.Core.Forge.Responses;
using SPTarkov.Core.SPT;
using SPTarkov.Core.SPT.Responses;

namespace SPTarkov.Core.Helpers;

public class HttpHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpHelper> _logger;
    private readonly StateHelper _stateHelper;
    private readonly ForgeRateLimiter _rateLimiter;

    public HttpHelper(ILogger<HttpHelper> logger, StateHelper stateHelper, ForgeRateLimiter rateLimiter)
    {
        _logger = logger;
        _stateHelper = stateHelper;
        _rateLimiter = rateLimiter;

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = CertificateValidationCallback;
        handler.UseCookies = false;

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestVersion = new Version(3, 0);
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    }

    private static readonly HttpRequestOptionsKey<bool> AllowUntrustedCertificate = new("AllowUntrustedCertificate");

    // Accepts untrusted certificates only for requests marked as game-server traffic (self-signed local SPT servers).
    private static bool CertificateValidationCallback(
        HttpRequestMessage httpRequestMessage,
        X509Certificate2? x509Certificate2,
        X509Chain? x509Chain,
        SslPolicyErrors sslPolicyErrors
    )
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        return httpRequestMessage.Options.TryGetValue(AllowUntrustedCertificate, out var allowUntrusted) && allowUntrusted;
    }

    private static HttpRequestMessage BuildGameServerMessage(HttpMethod methodType, string url, HttpContent? content = null)
    {
        var message = new HttpRequestMessage(methodType, url) { Content = content };
        message.Options.Set(AllowUntrustedCertificate, true);
        return message;
    }

    private string BuildGameUrl(string url)
    {
        return "https://" + _stateHelper.SelectedServer?.IpAddress + url;
    }

    public async Task<T?> GameServerGet<T>(string url, CancellationToken token)
    {
        _logger.LogDebug("Get: {Url}", url);

        var task = await _httpClient.SendAsync(BuildGameServerMessage(HttpMethod.Get, BuildGameUrl(url)), token);
        var json = SimpleZlib.Decompress(await task.Content.ReadAsByteArrayAsync(token));
        return JsonSerializer.Deserialize<T>(json);
    }

    // Pings a specific server address, independent of the currently-selected server, so a card can show live reachability
    // for a server it is not connected to. Any failure (unreachable, bad response, cancellation) reads as offline.
    public async Task<bool> PingServerAsync(string ipAddress, CancellationToken token)
    {
        try
        {
            var response = await _httpClient.SendAsync(BuildGameServerMessage(HttpMethod.Get, "https://" + ipAddress + Urls.Ping), token);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = SimpleZlib.Decompress(await response.Content.ReadAsByteArrayAsync(token));
            var ping = JsonSerializer.Deserialize<SPTPingResponse>(json);
            return ping?.Response == "Pong!";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "PingServerAsync failed for {IpAddress}", ipAddress);
            return false;
        }
    }

    public async Task<T?> GameServerPut<T>(string url, object request, CancellationToken token)
    {
        _logger.LogDebug("Put: {Url}", url);

        var content = new ByteArrayContent(SimpleZlib.CompressToBytes(JsonSerializer.Serialize(request), zlibConst.Z_BEST_COMPRESSION));

        var task = await _httpClient.SendAsync(BuildGameServerMessage(HttpMethod.Put, BuildGameUrl(url), content), token);

        return JsonSerializer.Deserialize<T>(SimpleZlib.Decompress(await task.Content.ReadAsByteArrayAsync(token)));
    }

    public async Task<ForgeModResponse?> ForgeGetMod(string? modId, CancellationToken token)
    {
        await _rateLimiter.WaitAsync(token);

        var paramsToUse = GetParamsCollection();
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeMod}/{modId}?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetMod Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeModResponse>(response);
    }

    public async Task<ForgeVersionResponse?> ForgeGetModVersion(string modId, string versionId, CancellationToken token)
    {
        await _rateLimiter.WaitAsync(token);

        var paramsToUse = ParamsCollectionForVersions(versionId);
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeMod}/{modId}/versions?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetModVersion Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeVersionResponse>(response);
    }

    public async Task<ForgeVersionResponse?> ForgeGetLatestCompatibleModVersion(string modId, CancellationToken token)
    {
        await _rateLimiter.WaitAsync(token);

        var paramsToUse = ParamsCollectionForVersions(sptVersion: ProgramStatics.SptVersionCompiledFor.ToString(), sort: "-version");
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeMod}/{modId}/versions?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetLatestCompatibleModVersion Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeVersionResponse>(response);
    }

    public async Task<ForgeModsResponse?> ForgeGetMods(
        CancellationToken token,
        string search = "",
        string sort = "-featured,name",
        int page = 1,
        string? includeFeatured = null,
        string? includeAi = null,
        string? category = null
    )
    {
        await _rateLimiter.WaitAsync(token);

        var paramsToUse = GetParamsCollection(search, sort, ConvertOptionToBool(includeFeatured), ConvertOptionToBool(includeAi), category);
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeMods}?page={page}&{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetMods Response: {Response}", response);

        if (task.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _httpClient.CancelPendingRequests();
            throw new ForgeRetryException("Too many requests made to the forge", null);
        }

        if (!task.IsSuccessStatusCode)
        {
            return new ForgeModsResponse { Success = false };
        }

        return JsonSerializer.Deserialize<ForgeModsResponse>(response);
    }

    public async Task<ForgeUpdateResponse?> ForgeGetUpdate(List<string> modGuidsWithVersions, string sptVersion, CancellationToken token)
    {
        await _rateLimiter.WaitAsync(token);

        var paramsToUse = ParamsCollectionForUpdates(modGuidsWithVersions, sptVersion);
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeUpdate}?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetUpdate Response: {Response}", response);

        if (!task.IsSuccessStatusCode)
        {
            return new ForgeUpdateResponse { Success = false };
        }

        return JsonSerializer.Deserialize<ForgeUpdateResponse>(response);
    }

    public async Task<ForgeAddonResponse?> ForgeGetModAddons(string modId, CancellationToken token)
    {
        await _rateLimiter.WaitAsync(token);

        var paramsToUse = ParamsCollectionForAddons(modId);
        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeAddons}?{paramsToUse}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetModAddons Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeAddonResponse>(response);
    }

    public async Task<ForgeAddonDetailsResponse?> ForgeGetModAddonDetails(string addonId, CancellationToken token)
    {
        await _rateLimiter.WaitAsync(token);

        var message = BuildMessage(HttpMethod.Get, $"{Urls.ForgeAddonDetails}/{addonId}");
        var task = await _httpClient.SendAsync(message, token);
        var response = await task.Content.ReadAsStringAsync(token);

        _logger.LogDebug("ForgeGetModAddon Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeAddonDetailsResponse>(response);
    }

    public async Task<ForgeCategoriesResponse?> ForgeGetCategories(CancellationToken tokenToken)
    {
        await _rateLimiter.WaitAsync(tokenToken);

        var message = BuildMessage(HttpMethod.Get, Urls.ForgeCategories);
        var task = await _httpClient.SendAsync(message, tokenToken);
        var response = await task.Content.ReadAsStringAsync(tokenToken);

        _logger.LogDebug("ForgeGetCategories Response: {Response}", response);

        return JsonSerializer.Deserialize<ForgeCategoriesResponse>(response);
    }

    public async Task<bool> ForgePing(CancellationToken token = default)
    {
        try
        {
            await _rateLimiter.WaitAsync(token);

            var message = BuildMessage(HttpMethod.Get, Urls.ForgePing);
            var response = await _httpClient.SendAsync(message, token);

            _logger.LogDebug("ForgePing: {StatusCode}", response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "ForgePing failed");
            return false;
        }
    }

    private NameValueCollection GetParamsCollection(
        string? search = null,
        string? sort = null,
        bool? featured = null,
        bool? ai = null,
        string? category = null
    )
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

        if (category is not null && !string.Equals(category, "all-cat", StringComparison.OrdinalIgnoreCase))
        {
            // must be lower case
            queryString.Add("filter[category_slug]", category);
        }

        queryString.Add("filter[spt_version]", ProgramStatics.SptVersionCompiledFor.ToString());

        if (!string.IsNullOrWhiteSpace(sort))
        {
            queryString.Add("sort", sort);
        }

        return queryString;
    }

    private NameValueCollection ParamsCollectionForVersions(string? versionId = null, string? sptVersion = null, string? sort = null)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("include", "dependencies,virus_total_links");

        if (!string.IsNullOrEmpty(versionId))
        {
            queryString.Add("filter[id]", versionId);
        }

        if (!string.IsNullOrEmpty(sptVersion))
        {
            queryString.Add("filter[spt_version]", sptVersion);
        }

        if (!string.IsNullOrEmpty(sort))
        {
            queryString.Add("sort", sort);
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

    private HttpRequestMessage BuildMessage(HttpMethod methodType, string url)
    {
        var message = new HttpRequestMessage(methodType, url);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.UserAgent.Add(
            new ProductInfoHeaderValue(
                "SinglePlayerTarkovLauncher",
                $"SPT-{ProgramStatics.SptVersionCompiledFor}-{ProgramStatics.SptCommit}"
            )
        );

        return message;
    }
}
