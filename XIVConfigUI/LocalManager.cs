using Dalamud;
using Dalamud.Utility;
using Newtonsoft.Json;
using System.ComponentModel;

namespace XIVConfigUI;

/// <summary>
/// 
/// </summary>
public static class LocalManager
{
    private static Dictionary<string, string> RightLang = [];

    /// <summary>
    /// 
    /// </summary>
    public static event Action? OnLanguageChanged;

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = [];

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
        var key = (member.DeclaringType?.FullName ?? string.Empty) + suffix + "." + member.Name;
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
        if(!string.IsNullOrEmpty(@default))
        {
            RightLang[key] = @default;
        }
#else
        if (RightLang.TryGetValue(key, out var value)) return value;
#endif
        return @default;
    }

    internal static void InIt()
    {
#if DEBUG
        var dirInfo = Service.PluginInterface.AssemblyLocation.Directory;
        dirInfo = dirInfo?.Parent!.Parent!.Parent!;

        var directory = dirInfo.FullName + @"\Localization";
        if (!Directory.Exists(directory)) return;

        if (Service.PluginInterface.UiLanguage != "en") return;

        //Default values.
        var path = Path.Combine(directory, "Localization.json");

        if (File.Exists(path))
        {
            RightLang = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];
        }

        if (RightLang.Count == 0)
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

        if (!Directory.Exists(directory))
        {
            Service.Log.Warning($"Failed to find the path \"{directory}\" to save the Localization.json");
            return;
        }

        if (Service.PluginInterface.UiLanguage != "en") return;

        //Default values.
        var path = Path.Combine(directory, "Localization.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(RightLang, Formatting.Indented));

        Service.Log.Info("Exported the json file");
    }
#endif
    private static async void SetLanguage(string lang)
    {
        if (Translations.TryGetValue(lang, out var value))
        {
            RightLang = value;
        }
        else
        {
            try
            {
                var url = $"https://raw.githubusercontent.com/{XIVConfigUIMain.UserName}/{XIVConfigUIMain.RepoName}/main/{XIVConfigUIMain.RepoName}/Localization/{lang}.json";
                using var client = new HttpClient();
                RightLang = Translations[lang] = JsonConvert.DeserializeObject<Dictionary<string, string>>(await client.GetStringAsync(url))!;
            }
            catch (HttpRequestException ex) when (ex?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Service.Log.Information(ex, $"No language {lang}");
                RightLang = [];
            }
            catch (Exception ex)
            {
                Service.Log.Warning(ex, $"Failed to download the language {lang}");
                RightLang = [];
            }
        }

        XIVConfigUIMain.DisableCommand();
        XIVConfigUIMain.EnableCommand();
        OnLanguageChanged?.Invoke();
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
