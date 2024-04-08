using Dalamud.Utility;
using Newtonsoft.Json;
using System.ComponentModel;

namespace XIVConfigUI;

/// <summary>
/// 
/// </summary>
public static class LocalManager
{
    private static string _userName = "", _repoName = "";
    private static Dictionary<string, string> _rightLang = [];

    /// <summary>
    /// 
    /// </summary>
    public static event Action? LanguageChanged;

    private static readonly Dictionary<string, Dictionary<string, string>> _translations = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="enum"></param>
    /// <param name="suffix"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string Local(this Enum @enum, string suffix = "", string value = "")
    {
        var key = (@enum.GetType().FullName ?? string.Empty) + suffix + "." + @enum.ToString();
        value = string.IsNullOrEmpty(value) ? @enum.GetAttribute<DescriptionAttribute>()?.Description ?? @enum.ToString()
            : value;
        return key.Local(value);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="member"></param>
    /// <param name="suffix"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string Local(this MemberInfo member, string suffix = "", string value = "")
    {
        var key = (member.DeclaringType?.FullName ?? string.Empty) + suffix + "." + member.ToString();
        value = string.IsNullOrEmpty(value) ? member.GetCustomAttribute<DescriptionAttribute>()?.Description ?? member.ToString()!
            : value;
        return key.Local(value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="suffix"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string Local(this Type type, string suffix = "", string value = "")
    {
        var key = (type.FullName ?? type.Name) + suffix;
        value = string.IsNullOrEmpty(value) ? type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.ToString()!
            : value;
        return key.Local(value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="default"></param>
    /// <returns></returns>
    public static string Local(this string key, string @default)
    {
#if DEBUG
        _rightLang[key] = @default;
#else
        if (_rightLang.TryGetValue(key, out var value)) return value;
#endif
        return @default;
    }

    internal static void InIt(string userName, string repoName)
    {
        _userName = userName;
        _repoName = repoName;
#if DEBUG
        var dirInfo = Service.PluginInterface.AssemblyLocation.Directory;
        dirInfo = dirInfo?.Parent!.Parent!.Parent!;

        var directory = dirInfo.FullName + @"\Localization";
        if (!Directory.Exists(directory)) return;

        if (Service.PluginInterface.UiLanguage != "en") return;

        //Default values.
        var path = Path.Combine(directory, "Localization.json");
        _rightLang = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];

        if (_rightLang.Count == 0)
        {
            Service.Log.Error("Load translations failed");
        }
#else
        SetLanguage(Service.PluginInterface.UiLanguage);
#endif
        Service.PluginInterface.LanguageChanged += OnLanguageChange;
    }

#if DEBUG
    private static void ExportLocalization()
    {
        var dirInfo = Service.PluginInterface.AssemblyLocation.Directory;
        dirInfo = dirInfo?.Parent!.Parent!.Parent!;

        var directory = dirInfo.FullName + @"\Localization";

        if (!Directory.Exists(directory)) return;

        if (Service.PluginInterface.UiLanguage != "en") return;

        //Default values.
        var path = Path.Combine(directory, "Localization.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(_rightLang, Formatting.Indented));

        Service.Log.Info("Exported the json file");
    }
#endif
    private static async void SetLanguage(string lang)
    {
        if (_translations.TryGetValue(lang, out var value))
        {
            _rightLang = value;
        }
        else
        {
            try
            {
                var url = $"https://raw.githubusercontent.com/{_userName}/{_repoName}/main/{_repoName}/Localization/{lang}.json";
                using var client = new HttpClient();
                _rightLang = _translations[lang] = JsonConvert.DeserializeObject<Dictionary<string, string>>(await client.GetStringAsync(url))!;
            }
            catch (HttpRequestException ex) when (ex?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Service.Log.Information(ex, $"No language {lang}");
                _rightLang = [];
            }
            catch (Exception ex)
            {
                Service.Log.Warning(ex, $"Failed to download the language {lang}");
                _rightLang = [];
            }
        }

        LanguageChanged?.Invoke();
    }

    internal static void Dispose()
    {
        Service.PluginInterface.LanguageChanged -= OnLanguageChange;
#if DEBUG
        ExportLocalization();
#endif
    }

    private static void OnLanguageChange(string languageCode)
    {
        try
        {
            Service.Log.Information($"Loading Localization for {languageCode}");
            SetLanguage(languageCode);
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Unable to Load Localization");
        }
    }
}