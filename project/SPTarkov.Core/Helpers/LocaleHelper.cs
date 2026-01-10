using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Helpers;

/// <summary>
/// This will only be used for front facing UI elements, Logging will all be EN
/// </summary>
public class LocaleHelper
{
    private readonly Lock _lock = new();
    private readonly ILogger<LocaleHelper> _logger;
    private readonly ConfigHelper _configHelper;
    private readonly string _defaultLocale;
    private readonly List<Dictionary<string, string>> _listOfLocales = [];
    private Dictionary<string, string> _selectedLocale = new();
    private bool _logLocalesOne;

    public LocaleHelper(ILogger<LocaleHelper> logger, ConfigHelper configHelper)
    {
        _logger = logger;
        _configHelper = configHelper;
        _defaultLocale = _configHelper.GetConfig().Language;
        _logger.LogInformation("Default locale: {locale}", _defaultLocale);

        lock (_lock)
        {
            _logger.LogInformation("Loading locales from {dirPath}", Paths.LocalesPath);

            if (!Directory.Exists(Paths.LocalesPath))
            {
                _logger.LogCritical("Directory {dirPath} does not exist", Paths.LocalesPath);
                throw new Exception("Directory does not exist");
            }

            var files = Directory.GetFiles(Paths.LocalesPath, "*.json");
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var localeDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                _listOfLocales.Add(localeDict!);
            }

            SetLocale(_defaultLocale);
        }
    }

    public string Get(string key)
    {
        if (!_selectedLocale.TryGetValue(key, out var value))
        {
            _logger.LogError("Key {key} not found in locale {locale}", key, _selectedLocale["ietf_tag"]);
        }

        value ??= "Value was null, Please report this for fixing";
        return value;
    }

    public Dictionary<string, string> GetAvailableLocales()
    {
        if (!_logLocalesOne)
        {
            _logger.LogInformation("Available locales: {locales}", _listOfLocales.Select(x => x["ietf_tag"]));
            _logLocalesOne = true;
        }

        return _listOfLocales.ToDictionary(x => x["ietf_tag"], x => x["native_name"]);
    }

    public void SetLocale(string locale)
    {
        _selectedLocale = _listOfLocales.FirstOrDefault(x => x["ietf_tag"] == locale) ?? _listOfLocales.FirstOrDefault(x => x["ietf_tag"] == _defaultLocale)!;
        _configHelper.SetLocale(_selectedLocale.GetValueOrDefault("ietf_tag", "en"));
    }
}
