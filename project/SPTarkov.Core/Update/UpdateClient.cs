using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Update;

public class UpdateClient(ILogger<UpdateClient> logger, ConfigHelper configHelper)
{
    // Highest manifest schema this launcher knows how to read.
    private const int SupportedSchemaVersion = 1;

    // Uses the default handler, which validates certificates.
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Returns the newest applicable release, or <c>null</c> when there is nothing to offer. Transport and manifest failures throw.
    /// </summary>
    public async Task<AvailableUpdate?> CheckAsync(CancellationToken token)
    {
        var channel = configHelper.GetConfig().UpdateChannel;
        var manifest = await FetchVerifiedManifest(channel, token);
        if (manifest is null)
        {
            return null;
        }

        if (IsRollback(channel, manifest))
        {
            logger.LogWarning("Update manifest on the {Channel} channel is older than the last one seen; ignoring.", channel);
            return null;
        }

        configHelper.SetLastSeenManifestUtc(channel, manifest.GeneratedUtc);
        return SelectCandidate(manifest);
    }

    private async Task<UpdateManifest?> FetchVerifiedManifest(UpdateChannel channel, CancellationToken token)
    {
        var channelName = channel == UpdateChannel.Edge ? "edge" : "stable";
        var (manifestUrl, signatureUrl) =
            channel == UpdateChannel.Edge
                ? (Urls.UpdateManifestEdge, Urls.UpdateManifestEdgeSignature)
                : (Urls.UpdateManifestStable, Urls.UpdateManifestStableSignature);

        using var manifestResponse = await _httpClient.GetAsync(manifestUrl, token);

        // Treats a missing manifest as a channel with no releases yet.
        if (manifestResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        manifestResponse.EnsureSuccessStatusCode();

        var manifestBytes = await manifestResponse.Content.ReadAsByteArrayAsync(token);

        using var signatureResponse = await _httpClient.GetAsync(signatureUrl, token);
        signatureResponse.EnsureSuccessStatusCode();

        var signature = Convert.FromBase64String((await signatureResponse.Content.ReadAsStringAsync(token)).Trim());

        if (!ManifestSignature.Verify(manifestBytes, signature, ProgramStatics.UpdateSigningPublicKey))
        {
            throw new InvalidOperationException("Update manifest signature verification failed.");
        }

        var manifest =
            JsonSerializer.Deserialize<UpdateManifest>(manifestBytes) ?? throw new InvalidOperationException("Update manifest is empty.");

        if (!manifest.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Update manifest is for the '{manifest.Channel}' channel, not the requested '{channelName}' channel."
            );
        }

        if (manifest.SchemaVersion > SupportedSchemaVersion)
        {
            logger.LogWarning(
                "Update manifest schema version {Version} is newer than this launcher supports ({Supported}); ignoring.",
                manifest.SchemaVersion,
                SupportedSchemaVersion
            );
            return null;
        }

        return manifest;
    }

    private bool IsRollback(UpdateChannel channel, UpdateManifest manifest)
    {
        var lastSeen = configHelper.GetLastSeenManifestUtc(channel);
        return lastSeen is not null && manifest.GeneratedUtc < lastSeen;
    }

    private static AvailableUpdate? SelectCandidate(UpdateManifest manifest)
    {
        var current = ProgramStatics.LauncherVersion;
        var buildUtc = ProgramStatics.LauncherBuildUtc;

        var candidate = manifest
            .Releases.Where(release => !release.Yanked)
            .Where(release => ChannelAllowed(release, manifest.Channel))
            .Where(release => OnSameLine(release, current))
            .Where(release => Version.Parse(release.LauncherVersion) > current)
            .Where(release => release.PublishedUtc.UtcDateTime > buildUtc)
            .OrderByDescending(release => Version.Parse(release.LauncherVersion))
            .FirstOrDefault();

        return candidate is null ? null : new AvailableUpdate { Release = candidate, CurrentVersion = current };
    }

    private static bool ChannelAllowed(ReleaseEntry release, string channel)
    {
        return release.Channel.Equals("stable", StringComparison.OrdinalIgnoreCase)
            || release.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase);
    }

    private static bool OnSameLine(ReleaseEntry release, Version current)
    {
        var version = Version.Parse(release.LauncherVersion);
        return version.Major == current.Major && version.Minor == current.Minor && version.Build == current.Build;
    }
}
