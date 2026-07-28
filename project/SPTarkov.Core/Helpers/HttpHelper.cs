using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Pings a specific server address, independent of the currently-selected server. Any failure (unreachable, bad response,
    /// cancellation) reads as offline.
    /// </summary>
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
        var paramsToUse = GetParamsCollection();
        return await ForgeGet<ForgeModResponse>($"{Urls.ForgeMod}/{modId}?{paramsToUse}", token);
    }

    public async Task<ForgeVersionResponse?> ForgeGetModVersion(string modId, string versionId, CancellationToken token)
    {
        var paramsToUse = ParamsCollectionForVersions(versionId);
        return await ForgeGet<ForgeVersionResponse>($"{Urls.ForgeMod}/{modId}/versions?{paramsToUse}", token);
    }

    public async Task<ForgeVersionResponse?> ForgeGetLatestCompatibleModVersion(string modId, CancellationToken token)
    {
        var paramsToUse = ParamsCollectionForVersions(sptVersion: ProgramStatics.SptVersionCompiledFor.ToString(), sort: "-version");
        return await ForgeGet<ForgeVersionResponse>($"{Urls.ForgeMod}/{modId}/versions?{paramsToUse}", token);
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
        var paramsToUse = GetParamsCollection(search, sort, ConvertOptionToBool(includeFeatured), ConvertOptionToBool(includeAi), category);
        return await ForgeGet<ForgeModsResponse>($"{Urls.ForgeMods}?page={page}&{paramsToUse}", token);
    }

    public async Task<ForgeUpdateResponse?> ForgeGetUpdate(List<string> modGuidsWithVersions, string sptVersion, CancellationToken token)
    {
        var paramsToUse = ParamsCollectionForUpdates(modGuidsWithVersions, sptVersion);
        return await ForgeGet<ForgeUpdateResponse>($"{Urls.ForgeUpdate}?{paramsToUse}", token);
    }

    public async Task<ForgeAddonResponse?> ForgeGetModAddons(string modId, CancellationToken token)
    {
        var paramsToUse = ParamsCollectionForAddons(modId);
        return await ForgeGet<ForgeAddonResponse>($"{Urls.ForgeAddons}?{paramsToUse}", token);
    }

    public async Task<ForgeAddonDetailsResponse?> ForgeGetModAddonDetails(string addonId, CancellationToken token)
    {
        return await ForgeGet<ForgeAddonDetailsResponse>($"{Urls.ForgeAddonDetails}/{addonId}", token);
    }

    public async Task<ForgeCategoriesResponse?> ForgeGetCategories(CancellationToken token)
    {
        return await ForgeGet<ForgeCategoriesResponse>(Urls.ForgeCategories, token);
    }

    // Gets the verified archive file listing for a mod version. A 404 means verification is not available.
    public async Task<ForgeFileTreeResponse?> ForgeGetModVersionFileTree(int modId, int versionId, CancellationToken token)
    {
        using var response = await ForgeSend($"{Urls.ForgeMod}/{modId}/versions/{versionId}/file-tree", token);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("No file tree available for mod {ModId} version {VersionId}", modId, versionId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ForgeGetModVersionFileTree failed with status code {StatusCode}", response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(token);
        return JsonSerializer.Deserialize<ForgeFileTreeResponse>(body);
    }

    public async Task<bool> ForgePing(CancellationToken token = default)
    {
        try
        {
            var response = await ForgeSend(Urls.ForgePing, token);

            _logger.LogDebug("ForgePing: {StatusCode}", response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "ForgePing failed");
            return false;
        }
    }

    private const int MaxRateLimitRetries = 3;
    private static readonly TimeSpan MaxRetryAfterDelay = TimeSpan.FromSeconds(30);

    // Sends a rate-limited Forge request, retrying 429 responses after the server's Retry-After delay.
    private async Task<HttpResponseMessage> ForgeSend(string url, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            await _rateLimiter.WaitAsync(token);

            var response = await _httpClient.SendAsync(BuildMessage(HttpMethod.Get, url), token);
            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                _rateLimiter.ClearServerRateLimit();
                return response;
            }

            var delay = RetryDelay(response, attempt);
            _rateLimiter.ReportServerRateLimit(delay);

            if (attempt >= MaxRateLimitRetries)
            {
                return response;
            }

            _logger.LogWarning(
                "Forge rate limited request to {Url}, retrying in {Delay:0.#}s (attempt {Attempt}/{MaxAttempts})",
                url,
                delay.TotalSeconds,
                attempt + 1,
                MaxRateLimitRetries
            );
            response.Dispose();
            await Task.Delay(delay, token);
        }
    }

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        var delay = retryAfter?.Delta ?? retryAfter?.Date - DateTimeOffset.UtcNow;
        if (delay is not { } serverDelay || serverDelay <= TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(1 << attempt);
        }

        return serverDelay < MaxRetryAfterDelay ? serverDelay : MaxRetryAfterDelay;
    }

    private async Task<T?> ForgeGet<T>(string url, CancellationToken token, [CallerMemberName] string caller = "")
        where T : class
    {
        var response = await ForgeSend(url, token);
        var body = await response.Content.ReadAsStringAsync(token);

        _logger.LogDebug("{Caller} Response: {Response}", caller, body);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("{Caller} failed with status code {StatusCode}", caller, response.StatusCode);
            return null;
        }

        return JsonSerializer.Deserialize<T>(body);
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
