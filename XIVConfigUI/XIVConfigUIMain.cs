using Dalamud.Game.Command;
using Dalamud.Plugin;
using Newtonsoft.Json.Linq;

namespace XIVConfigUI;

/// <summary>
/// The major class.
/// </summary>
public static class XIVConfigUIMain
{
    private static bool _inited = false;
    internal readonly static List<SearchableCollection> _searchableCollections = [];
    private static string _punchline = string.Empty, _descirption = string.Empty, _iconUrl = string.Empty;

    public static Func<bool> ShowTooltip { get; set; } = () => true;

    internal static string CommandForChangingSetting { get; private set; } = "/ConfigUI";

    public static string Punchline => (Service.PluginInterface.InternalName + "." + nameof(Punchline)).Local(_punchline);
    public static string Description => (Service.PluginInterface.InternalName + "." + nameof(Description)).Local(_descirption);
    public static string IconUrl => (Service.PluginInterface.InternalName + "." + nameof(IconUrl)).Local(_iconUrl);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pluginInterface"></param>
    /// <param name="userName"></param>
    /// <param name="repoName"></param>
    /// <param name="commandForChangingSetting"></param>
    public static void Init(DalamudPluginInterface pluginInterface, string userName, string repoName, string commandForChangingSetting)
    {
        if (_inited) return;
        _inited = true;

        pluginInterface.Create<Service>();
        LocalManager.InIt(userName, repoName);
        ImageLoader.Init();

        CommandForChangingSetting = commandForChangingSetting;

        Service.Commands.AddHandler(CommandForChangingSetting, new CommandInfo(OnCommand)
        {
            ShowInHelp = false,
        });

        var items = pluginInterface.AssemblyLocation.FullName.Split('.');
        items[^1] = "json";
        var path = string.Join('.', items);
        var obj = JObject.Parse(File.ReadAllText(path));
        _descirption = obj[nameof(Description)]?.ToString() ?? string.Empty;
        _punchline = obj[nameof(Punchline)]?.ToString() ?? string.Empty;
        _iconUrl = obj[nameof(IconUrl)]?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Don't forget to dispose this!
    /// </summary>
    public static void Dispose()
    {
        if (!_inited) return;
        _inited = false;

        _searchableCollections.Clear();
        Service.Commands.RemoveHandler(CommandForChangingSetting);
        LocalManager.Dispose();
    }

    private static void OnCommand(string command, string arguments)
    {
        foreach (var collection in _searchableCollections)
        {
            foreach (var item in collection)
            {
                if (string.IsNullOrEmpty(item.Command)) continue;
                if (!arguments.StartsWith(item.LeadingCommand)) continue;

                arguments = arguments[item.LeadingCommand.Length..].Trim();

                item.OnCommand(arguments);
                item._config?.AfterConfigChange(item);

                Service.Log.Debug($"Set the property \"{item._property.Name}\" to \"{item._property.GetValue(item._obj)?.ToString() ?? string.Empty}\"");

                return;
            }
        }

        Service.Log.Info($"Failed to find the config from \"{arguments}\"!");
    }
}
