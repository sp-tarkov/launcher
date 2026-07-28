using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Mods;

/// <summary>
/// Persists the tracked-mods dictionary to an encrypted data file in the per-install launcher data folder.
/// </summary>
public class ModTrackingStore
{
    private const string EncryptionKey = "do-not-modify-the-dat-file"; // simple tamper deterrent
    private const byte FormatVersion = 1;
    private static readonly byte[] _magic = "SPTM"u8.ToArray();

    private readonly ILogger<ModTrackingStore> _logger;
    private readonly Lock _lock = new();
    private Dictionary<string, ConfigMod> _mods = new();

    /// <summary>Loads the tracked mods from the data file on construction.</summary>
    public ModTrackingStore(ILogger<ModTrackingStore> logger)
    {
        _logger = logger;
        Load();
    }

    /// <summary>Gets the tracked mods, keyed by mod GUID.</summary>
    public Dictionary<string, ConfigMod> GetMods()
    {
        lock (_lock)
        {
            return _mods;
        }
    }

    /// <summary>Adds or replaces a tracked mod and saves the store.</summary>
    public void AddMod(ConfigMod mod)
    {
        lock (_lock)
        {
            _logger.LogDebug("AddMod: {Mod}", mod.Name);
            _mods[mod.GUID] = mod;
            Save();
        }
    }

    /// <summary>Removes a tracked mod by GUID and saves the store.</summary>
    public void RemoveMod(string guid)
    {
        lock (_lock)
        {
            _logger.LogDebug("RemoveMod: {Mod}", guid);
            if (!_mods.Remove(guid))
            {
                _logger.LogError("key {key} not found", guid);
            }

            Save();
        }
    }

    /// <summary>Reads and decrypts the data file into memory. A missing or unreadable file starts an empty store.</summary>
    private void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(Paths.ModsDataPath))
            {
                return;
            }

            try
            {
                var payload = Decrypt(File.ReadAllBytes(Paths.ModsDataPath));
                _mods = JsonSerializer.Deserialize<Dictionary<string, ConfigMod>>(payload) ?? new Dictionary<string, ConfigMod>();
            }
            catch (Exception e)
            {
                _logger.LogWarning("Unable to read the mods data file, starting with an empty mod list: {message}", e.Message);
                _mods = new Dictionary<string, ConfigMod>();
            }
        }
    }

    /// <summary>Encrypts the tracked mods and writes them to the data file through a temp-file swap.</summary>
    private void Save()
    {
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(Paths.LauncherDataPath);

                var payload = Encrypt(JsonSerializer.Serialize(_mods));
                var tempPath = Paths.ModsDataPath + ".tmp";
                File.WriteAllBytes(tempPath, payload);
                File.Move(tempPath, Paths.ModsDataPath, true);
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to write the mods data file: {message}", e.Message);
            }
        }
    }

    /// <summary>Encrypts a JSON payload into the framed file format: magic bytes, format version, IV, then AES-CBC ciphertext.</summary>
    private static byte[] Encrypt(string json)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(EncryptionKey));
        aes.GenerateIV();

        var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(json), aes.IV);

        var payload = new byte[_magic.Length + 1 + aes.IV.Length + cipher.Length];
        _magic.CopyTo(payload, 0);
        payload[_magic.Length] = FormatVersion;
        aes.IV.CopyTo(payload, _magic.Length + 1);
        cipher.CopyTo(payload, _magic.Length + 1 + aes.IV.Length);

        return payload;
    }

    /// <summary>Validates the file frame and decrypts the payload back to JSON. Throws on an unrecognized format.</summary>
    private static string Decrypt(byte[] payload)
    {
        var headerLength = _magic.Length + 1 + 16;
        if (
            payload.Length < headerLength
            || !payload.AsSpan(0, _magic.Length).SequenceEqual(_magic)
            || payload[_magic.Length] != FormatVersion
        )
        {
            throw new InvalidDataException("Unrecognized mods data file format");
        }

        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(EncryptionKey));

        var iv = payload.AsSpan(_magic.Length + 1, 16).ToArray();
        var plain = aes.DecryptCbc(payload.AsSpan(headerLength).ToArray(), iv);

        return Encoding.UTF8.GetString(plain);
    }
}
