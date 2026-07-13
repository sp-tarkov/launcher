using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Core.Update;

namespace SPTarkov.Tools.LauncherManifest;

// Appends one launcher release to the channel manifest and writes a detached signature for each.
internal static class ManifestBuilder
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static int Run(string[] args)
    {
        var cli = CommandLine.Parse(args);

        if (cli.Optional("check-payload") is { } payloadPath)
        {
            CheckPayload(payloadPath);
            return 0;
        }

        var inDir = cli.Required("in-dir");
        var outDir = cli.Required("out-dir");
        var privateKeyPem = File.ReadAllText(cli.Required("private-key-file"));
        Directory.CreateDirectory(outDir);

        if (cli.Optional("yank") is { } yankVersion)
        {
            Yank(yankVersion, inDir, outDir, privateKeyPem);
        }
        else
        {
            Append(BuildEntry(cli), inDir, outDir, privateKeyPem);
        }

        return 0;
    }

    // Applies the launcher's payload rules to a release zip.
    private static void CheckPayload(string path)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(path);

        var rejected = UpdatePayload.DisallowedEntries(archive.Entries.Select(entry => entry.FullName));
        if (rejected.Count > 0)
        {
            throw new ManifestException("the payload holds entries the launcher would reject: " + string.Join(", ", rejected));
        }

        Console.WriteLine($"{path} passes the launcher's payload rules ({archive.Entries.Count} entries)");
    }

    private static void Append(ReleaseEntry entry, string inDir, string outDir, string privateKeyPem)
    {
        foreach (var fileName in TargetsFor(entry.Channel))
        {
            var channel = Path.GetFileNameWithoutExtension(fileName);
            var manifest = Load(Path.Combine(inDir, fileName), channel);
            Write(Add(manifest, entry), Path.Combine(outDir, fileName), privateKeyPem);
        }
    }

    // Marks the version yanked in every manifest that lists it and bumps generatedUtc.
    private static void Yank(string version, string inDir, string outDir, string privateKeyPem)
    {
        if (!Version.TryParse(version, out var target))
        {
            throw new ManifestException($"'{version}' is not a launcher version");
        }

        var found = false;

        foreach (var fileName in new[] { "stable.json", "edge.json" })
        {
            var path = Path.Combine(inDir, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            var manifest = Load(path, Path.GetFileNameWithoutExtension(fileName));
            if (!manifest.Releases.Any(release => Version.Parse(release.LauncherVersion) == target))
            {
                continue;
            }

            found = true;
            var yanked = manifest with
            {
                GeneratedUtc = DateTimeOffset.UtcNow,
                Releases = manifest
                    .Releases.Select(release => Version.Parse(release.LauncherVersion) == target ? release with { Yanked = true } : release)
                    .ToList(),
            };
            Write(yanked, Path.Combine(outDir, fileName), privateKeyPem);
        }

        if (!found)
        {
            throw new ManifestException($"launcher version {version} is not in any manifest");
        }
    }

    // A stable release lands in both manifests; an edge release only in edge.
    private static string[] TargetsFor(string channel)
    {
        return channel switch
        {
            "stable" => ["stable.json", "edge.json"],
            "edge" => ["edge.json"],
            _ => throw new ManifestException($"unknown channel '{channel}'"),
        };
    }

    private static ReleaseEntry BuildEntry(CommandLine cli)
    {
        var launcherVersion = cli.Required("launcher-version");
        var sptVersion = cli.Required("spt-version");
        var version = Version.Parse(launcherVersion);

        if (version.Revision <= 0)
        {
            throw new ManifestException(
                $"launcher-version {launcherVersion} has revision {version.Revision}; auto-update revisions start at .1"
            );
        }

        if ($"{version.Major}.{version.Minor}.{version.Build}" != sptVersion)
        {
            throw new ManifestException($"launcher-version {launcherVersion} does not sit on spt-version {sptVersion}");
        }

        return new ReleaseEntry
        {
            LauncherVersion = launcherVersion,
            SptVersion = sptVersion,
            Channel = cli.Required("channel"),
            Tag = cli.Required("tag"),
            Commit = cli.Optional("commit"),
            PublishedUtc = ParseUtc(cli.Required("published-utc")),
            Yanked = false,
            DeltaSha256 = cli.Optional("delta-sha256"),
            NotesUrl = cli.Optional("notes-url"),
            Notes = ReadNotes(cli),
            Asset = new ReleaseAsset
            {
                Url = cli.Required("asset-url"),
                Size = long.Parse(cli.Required("asset-size"), CultureInfo.InvariantCulture),
                Sha256 = cli.Required("asset-sha256"),
            },
        };
    }

    private static string? ReadNotes(CommandLine cli)
    {
        var notesFile = cli.Optional("notes-file");
        return notesFile is not null ? File.ReadAllText(notesFile) : cli.Optional("notes");
    }

    private static UpdateManifest Load(string path, string channel)
    {
        if (!File.Exists(path))
        {
            return new UpdateManifest { Channel = channel };
        }

        return JsonSerializer.Deserialize<UpdateManifest>(File.ReadAllText(path)) ?? new UpdateManifest { Channel = channel };
    }

    private static UpdateManifest Add(UpdateManifest manifest, ReleaseEntry entry)
    {
        GuardDuplicate(manifest, entry);
        GuardDelta(manifest, entry);

        return manifest with
        {
            GeneratedUtc = DateTimeOffset.UtcNow,
            Releases = manifest.Releases.Append(entry).OrderByDescending(release => Version.Parse(release.LauncherVersion)).ToList(),
        };
    }

    private static void Write(UpdateManifest manifest, string path, string privateKeyPem)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, _serializerOptions);
        File.WriteAllBytes(path, bytes);
        File.WriteAllText(path + ".sig", Convert.ToBase64String(ManifestSignature.Sign(bytes, privateKeyPem)));
        Console.WriteLine($"wrote {path} ({manifest.Releases.Count} releases) and {Path.GetFileName(path)}.sig");
    }

    private static void GuardDuplicate(UpdateManifest manifest, ReleaseEntry entry)
    {
        if (manifest.Releases.Any(release => release.LauncherVersion == entry.LauncherVersion))
        {
            throw new ManifestException($"{manifest.Channel}.json already contains launcher version {entry.LauncherVersion}");
        }
    }

    // Rejects an entry whose delta hash differs from the newest release on the same SPT line.
    private static void GuardDelta(UpdateManifest manifest, ReleaseEntry entry)
    {
        var newest = manifest
            .Releases.Where(release => OnSameLine(release, entry.SptVersion))
            .OrderByDescending(release => Version.Parse(release.LauncherVersion))
            .FirstOrDefault();

        if (
            newest?.DeltaSha256 is { } previous
            && entry.DeltaSha256 is { } incoming
            && !string.Equals(previous, incoming, StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new ManifestException(
                $"delta hash changed on line {entry.SptVersion} ({previous} -> {incoming}); a new client delta belongs to a full SPT release"
            );
        }
    }

    private static bool OnSameLine(ReleaseEntry entry, string line)
    {
        var version = Version.Parse(entry.LauncherVersion);
        return $"{version.Major}.{version.Minor}.{version.Build}" == line;
    }

    private static DateTimeOffset ParseUtc(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
