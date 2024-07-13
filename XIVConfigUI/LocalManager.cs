using Dalamud.Utility;
using System.ComponentModel;
using XIVConfigUI.Attributes;

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
        if (@enum.GetType().GetCustomAttribute<FlagsAttribute>() == null) return @enum.LocalRaw(suffix, value);

        var definedValues = @enum.GetType().GetCleanedEnumValues();

        List<string> values = [];
        foreach (Enum definedValue in definedValues)
        {
            if (Convert.ToInt32(definedValue) == 0) continue;
            if (!@enum.HasFlag(definedValue)) continue;

            values.Add(definedValue.LocalRaw(suffix, value));
        }

        return values.Count == 0 ? LocalString.None.Local() : string.Join(", ", values);
    }

    private static string LocalRaw(this Enum @enum, string suffix, string value)
    {
        var key = GetTypeName(@enum.GetType()) + suffix + "." + @enum.ToString();
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
        var key = GetTypeName(member.DeclaringType) + suffix + "." + member.Name;
        value = string.IsNullOrEmpty(value) ? member.GetCustomAttribute<DescriptionAttribute>()?.Description ?? member.Name!
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
        var key = GetTypeName(type) + suffix;
        value = string.IsNullOrEmpty(value) ? type.GetCustomAttribute<DescriptionAttribute>()?.Description 
            ?? type.Name : value;
        return key.Local(value);
    }

    private static string GetTypeName(Type? type)
    {
        if (type == null) return string.Empty;
        return (type.FullName ?? type.Name).Split('`').First();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public static string LocalUINameDesc(this MemberInfo member)
    {
        var desc = member.LocalUIName();
        var relay = member.LocalUIDesc();
        if (!string.IsNullOrEmpty(relay))
        {
            desc += "\n" + relay;
        }
        return desc;
    }

    /// <summary>
    /// Get the ui name of the property.
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public static string LocalUIName(this MemberInfo member)
    {
        var ui = member.GetCustomAttribute<UIAttribute>();

        if (ui == null) return string.Empty;
        var name = string.IsNullOrEmpty(ui.Name) ? member.Name : ui.Name;

        return member.Local("Name", name);
    }

    /// <summary>
    /// Get the ui description of the property.
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public static string LocalUIDesc(this MemberInfo member)
    {
        var desc = member.LocalUIDescription();
        if(member.GetCustomAttribute<RangeAttribute>() is RangeAttribute range)
        {
            desc += "\n" + range.UnitType.Local();
        }
        return desc;
    }

    internal static string LocalUIDescription(this MemberInfo member)
    {
        var ui = member.GetCustomAttribute<UIAttribute>();
        if (ui == null || string.IsNullOrEmpty(ui.Description)) return string.Empty;

        return member.Local("Description", ui.Description);
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

    internal static void InIt(params Type[] initTypes)
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
            RightLang = JsonHelper.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path)) ?? [];
        }

        if (RightLang.Count == 0)
        {
            Service.Log.Error("Load translations failed");
        }

        foreach (var type in initTypes
            .Append(typeof(LocalString))
            .Append(typeof(ConfigUnitType)))
        {
            if (type.IsEnum)
            {
                foreach (var value in Enum.GetValues(type))
                {
                    ((Enum)value).Local();
                }
            }
            else
            {
                foreach (var property in type.GetRuntimeProperties())
                {
                    property.LocalUIName();
                    property.LocalUIDescription();
                }
            }
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
        File.WriteAllText(path, JsonHelper.SerializeObject(RightLang));

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
                RightLang = Translations[lang] = JsonHelper.DeserializeObject<Dictionary<string, string>>(await client.GetStringAsync(url)) ?? [];
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
