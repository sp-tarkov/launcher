using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using ComponentAce.Compression.Libs.zlib;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.SPT;
using SPTarkov.Core.SPT.Responses;

namespace SPTarkov.Core.Helpers;

public class HttpHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpHelper> _logger;
    private readonly StateHelper _stateHelper;

    public HttpHelper(ILogger<HttpHelper> logger, StateHelper stateHelper)
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

    /// <summary>
    /// Pings a specific server address, independent of the currently-selected server. Any failure (unreachable, bad response,
    /// cancellation) reads as offline.
    /// </summary>
    public async Task<bool> PingServerAsync(string ipAddress, CancellationToken token)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://" + ipAddress + Urls.Ping, token);
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

        var task = await _httpClient.PutAsync(BuildGameUrl(url), content, token);

        return JsonSerializer.Deserialize<T>(SimpleZlib.Decompress(await task.Content.ReadAsByteArrayAsync(token)));
    }
}
