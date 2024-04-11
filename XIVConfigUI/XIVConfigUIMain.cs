using Dalamud.Game.Command;
using Dalamud.Plugin;
using Newtonsoft.Json.Linq;

namespace XIVConfigUI;

/// <summary>
/// The major class.
/// </summary>
public static class XIVConfigUIMain
{
    private static Action<string>? _onCommand;
    private static bool _inited = false;
    private static string _punchline = string.Empty, _descirption = string.Empty, _iconUrl = string.Empty;

    internal static string _userName = "", _repoName = "";
    internal static readonly List<SearchableCollection> _searchableCollections = [];

    /// <summary>
    /// Control if show tooltips.
    /// </summary>
    public static Func<bool> ShowTooltip { get; set; } = () => true;

    internal static string Command { get; private set; } = "/ConfigUI";
    internal static string DescriptionAboutCommand { get; private set; } = string.Empty;
    /// <summary>
    /// The punchline of this plugin.
    /// </summary>
    public static string Punchline => (Service.PluginInterface.InternalName + "." + nameof(Punchline)).Local(_punchline);

    /// <summary>
    /// The description of this plugin.
    /// </summary>
    public static string Description => (Service.PluginInterface.InternalName + "." + nameof(Description)).Local(_descirption);

    /// <summary>
    /// The icon of this plugin.
    /// </summary>
    public static string IconUrl => _iconUrl;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pluginInterface"></param>
    /// <param name="userName">the user name in github</param>
    /// <param name="repoName">the repo name in github</param>
    /// <param name="command">the command for changing config</param>
    /// <param name="descriptionAboutCommand"></param>
    /// <param name="onCommand"></param>
    public static void Init(DalamudPluginInterface pluginInterface, string userName, string repoName, string command,
        string? descriptionAboutCommand = null, Action<string>? onCommand = null)
    {
        if (_inited) return;
        _inited = true;

        pluginInterface.Create<Service>();
        _userName = userName;
        _repoName = repoName;
        _onCommand = onCommand;
        LocalManager.InIt();
        ImageLoader.Init();

        Command = command;
        DescriptionAboutCommand = descriptionAboutCommand ?? string.Empty;
        EnableCommand();

        var items = pluginInterface.AssemblyLocation.FullName.Split('.');
        items[^1] = "json";
        var path = string.Join('.', items);
        var obj = JObject.Parse(File.ReadAllText(path));
        _descirption = obj[nameof(Description)]?.ToString() ?? string.Empty;
        _punchline = obj[nameof(Punchline)]?.ToString() ?? string.Empty;
        _iconUrl = obj[nameof(IconUrl)]?.ToString() ?? string.Empty;
    }

    internal static void EnableCommand()
    {
        Service.Commands.AddHandler(Command, new CommandInfo(OnCommand)
        {
            ShowInHelp = !string.IsNullOrEmpty(DescriptionAboutCommand),
            HelpMessage = (Service.PluginInterface.InternalName + "." + nameof(DescriptionAboutCommand)).Local(DescriptionAboutCommand ?? string.Empty),
        });
    }

    internal static void DisableCommand()
    {
        Service.Commands.RemoveHandler(Command);
    }

    /// <summary>
    /// Don't forget to dispose this!
    /// </summary>
    public static void Dispose()
    {
        if (!_inited) return;
        _inited = false;

        _searchableCollections.Clear();
        DisableCommand();
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

        _onCommand?.Invoke(arguments);
    }
}
