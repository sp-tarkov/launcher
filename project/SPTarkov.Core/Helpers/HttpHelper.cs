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
using SPTarkov.Core.Logging;
using SPTarkov.Core.Models;

namespace SPTarkov.Core.Helpers;

public class HttpHelper
{
    private readonly ConfigHelper _configHelper;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpHelper> _logger;
    private readonly StateHelper _stateHelper;
    private bool _internetAccess;
    private string _token;

    public HttpHelper(
        ConfigHelper configHelper,
        ILogger<HttpHelper> logger,
        StateHelper stateHelper
    )
    {
        _configHelper = configHelper;
        _logger = logger;
        _stateHelper = stateHelper;

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = CertificateValidationCallback;
        handler.UseCookies = false;

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestVersion = new Version(3, 0);
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        _token = configHelper.GetConfig().ForgeApiKey;
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
        return "https://" + _stateHelper.SelectedServer.IpAddress + url;
    }

    public async Task<T?> GameServerGet<T>(string url, CancellationToken token)
    {
        _logger.LogInformation("GET: {Url}", url);
        var task = await _httpClient?.GetAsync(BuildGameUrl(url), token);

        var json = SimpleZlib.Decompress(
            await task.Content.ReadAsByteArrayAsync(token)
        );

        return JsonSerializer.Deserialize<T>(
            json
        );
    }

    public async Task<T?> GameServerPut<T>(string url, object request, CancellationToken token)
    {
        _logger.LogInformation("Put: {Url}", url);

        var content = new ByteArrayContent(
            SimpleZlib.CompressToBytes(
                JsonSerializer.Serialize(request)
                , zlibConst.Z_BEST_COMPRESSION
            )
        );

        var task = await _httpClient?.PutAsync(BuildGameUrl(url), content, token);

        return JsonSerializer.Deserialize<T>(
            SimpleZlib.Decompress(
                await task.Content.ReadAsByteArrayAsync(token)
            )
        );
    }

    public async Task<ForgeModResponse?> ForgeGetMod(string? modId, CancellationToken token)
    {
        _logger.LogInformation("forge GetModFromForge");

        if (string.IsNullOrWhiteSpace(_configHelper.GetConfig().ForgeApiKey))
        {
            _logger.LogWarning("GetMods - API Key is missing.");
            return null;
        }

        _logger.LogInformation("api key: {ForgeApiKey}", _configHelper.GetConfig().ForgeApiKey);

        var paramsToUse = GetParamsCollection();
        var message = new HttpRequestMessage(HttpMethod.Get, $"https://forge.sp-tarkov.com/api/v0/mod/{modId}?{paramsToUse}")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var task = await _httpClient?.SendAsync(message, token);
        return JsonSerializer.Deserialize<ForgeModResponse>(await task.Content.ReadAsStringAsync(token));
    }

    public async Task<ForgeVersionResponse?> ForgeGetModVersion(string modId, string versionId, CancellationToken token)
    {
        _logger.LogInformation("forge GetModVersionFromForge");

        if (string.IsNullOrWhiteSpace(_configHelper.GetConfig().ForgeApiKey))
        {
            _logger.LogWarning("GetMods - API Key is missing.");
            return null;
        }

        _logger.LogInformation("api key: {ForgeApiKey}", _configHelper.GetConfig().ForgeApiKey);

        var paramsToUse = GetParamsCollectionForVersions(versionId);
        var message = new HttpRequestMessage(HttpMethod.Get, $"https://forge.sp-tarkov.com/api/v0/mod/{modId}/versions?{paramsToUse}")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var task = await _httpClient?.SendAsync(message, token);
        return JsonSerializer.Deserialize<ForgeVersionResponse>(await task.Content.ReadAsStringAsync(token));
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
        _logger.LogInformation("forge GetModsFromForge");

        if (string.IsNullOrWhiteSpace(_configHelper.GetConfig().ForgeApiKey))
        {
            _logger.LogWarning("GetMods - API Key is missing.");
            return null;
        }

        _logger.LogInformation("api key: {ForgeApiKey}", _configHelper.GetConfig().ForgeApiKey);

        var paramsToUse = GetParamsCollection(search, sort, ConvertFeaturedToBool(includeFeatured), ConvertFeaturedToBool(includeAi));
        var message = new HttpRequestMessage(HttpMethod.Get, $"https://forge.sp-tarkov.com/api/v0/mods?page={page}&{paramsToUse}")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var task = await _httpClient?.SendAsync(message, token);

        if (!task.IsSuccessStatusCode)
        {
            // remove any api keys and get them to log back in.
            return new ForgeModsResponse
            {
                Success = false
            };
        }

        return JsonSerializer.Deserialize<ForgeModsResponse>(await task.Content.ReadAsStringAsync(token));
    }

    public async Task<ForgeLogoutResponse?> ForgeLogout(CancellationToken token)
    {
        _logger.LogInformation("Forge ForgeLogout");

        if (string.IsNullOrWhiteSpace(_configHelper.GetConfig().ForgeApiKey))
        {
            _logger.LogWarning("GetMods - API Key is missing.");
            return null;
        }

        var message = new HttpRequestMessage(HttpMethod.Post, "https://forge.sp-tarkov.com/api/v0/auth/logout")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        var task = await _httpClient?.SendAsync(message, token);
        return JsonSerializer.Deserialize<ForgeLogoutResponse>(await task.Content.ReadAsStringAsync(token));
    }

    public async Task<ForgeLoginResponse?> ForgeLogin(object request, CancellationToken token)
    {
        _logger.LogInformation("Forge ForgeLogin");

        var message = new HttpRequestMessage(HttpMethod.Post, "https://forge.sp-tarkov.com/api/v0/auth/login")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var task = await _httpClient?.SendAsync(message, token);
        return JsonSerializer.Deserialize<ForgeLoginResponse>(await task.Content.ReadAsStringAsync(token));
    }

    private NameValueCollection GetParamsCollection(string? search = null, string? sort = null, bool? featured = null, bool? ai = null)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("include", "versions,owner,authors,license");
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

        // make this dynamic later
        queryString.Add("filter[spt_version]", "4.0.0");

        if (!string.IsNullOrWhiteSpace(sort))
        {
            queryString.Add("sort", sort);
        }

        return queryString;
    }

    private NameValueCollection GetParamsCollectionForVersions(string? versionId = null)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);

        if (!string.IsNullOrEmpty(versionId))
        {
            queryString.Add("filter[id]", versionId);
        }

        return queryString;
    }

    private bool? ConvertFeaturedToBool(string selected)
    {
        switch (selected.ToLower())
        {
            case "exclude":
                return false;
            case "only":
                return true;
            case "include":
            default:
                return null;
        }
    }

    public string GetApiKey()
    {
        return _token;
    }

    public void ClearApiKey()
    {
        _logger.LogInformation("ClearApiKey");
        _token = "";
        _configHelper.SetApiKey(_token);
    }

    public void SetApiKey(string apiKey)
    {
        _token = apiKey;
        _configHelper.SetApiKey(_token);
    }

    public bool IsInternetAccessAvailable()
    {
        // change to just pinging forge https://forge.sp-tarkov.com/api/v0/ping?
        try
        {
            using (var ping = new Ping())
            {
                var result = ping.Send("8.8.8.8", 1000); // Google's DNS server
                _internetAccess = result.Status == IPStatus.Success;
            }
        }
        catch
        {
            _internetAccess = false;
        }

        _logger.LogInformation("IsInternetAccessAvailable: {InternetAccess}", _internetAccess);
        return _internetAccess;
    }


}
