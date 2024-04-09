﻿using Dalamud.Game.Command;
using Dalamud.Interface.Internal;
using Dalamud.Plugin;

namespace XIVConfigUI;

/// <summary>
/// The major class.
/// </summary>
public static class XIVConfigUIMain
{
    private static bool _inited = false;
    internal readonly static List<SearchableCollection> _searchableCollections = [];

    internal static SearchableConfig Config { get; private set; } = null!;

    internal static string CommandForChangingSetting { get; private set; } = "/ConfigUI";

    #region GetIcon
    internal static bool GetTexture(string path, out IDalamudTextureWrap texture, bool loadingIcon = false)
        => Config.GetTexture(path, out texture, loadingIcon);
    internal static bool GetTexture(uint id, out IDalamudTextureWrap texture, uint @default = 0)
        => Config.GetTexture(id, out texture, @default);

    private static readonly Dictionary<uint, uint> _actionIcons = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="texture"></param>
    /// <returns></returns>
    public static bool GetTextureAction(uint id, out IDalamudTextureWrap texture)
    {
        if (id == 0)
        {
            return GetTexture(0, out texture, 0);
        }
        if (id == 3)
        {
            return GetTexture(104, out texture, 0);
        }

        if (!_actionIcons.TryGetValue(id, out var iconId))
        {
            iconId = Service.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?
                .GetRow(id)?.Icon ?? 0;
            _actionIcons[id] = iconId;
        }
        return GetTexture(iconId, out texture);
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pluginInterface"></param>
    /// <param name="userName"></param>
    /// <param name="repoName"></param>
    /// <param name="commandForChangingSetting"></param>
    /// <param name="searchConfig"></param>
    public static void Init(DalamudPluginInterface pluginInterface, string userName, string repoName,
        string commandForChangingSetting, SearchableConfig searchConfig)
    {
        if (_inited) return;
        _inited = true;

        pluginInterface.Create<Service>();
        LocalManager.InIt(userName, repoName);
        CommandForChangingSetting = commandForChangingSetting;
        Config = searchConfig;

        Service.Commands.AddHandler(CommandForChangingSetting, new CommandInfo(OnCommand)
        {
            ShowInHelp = false,
        });
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

                Service.Log.Debug($"Set the property \"{item._property.Name}\" to \"{item._property.GetValue(item._obj)?.ToString() ?? string.Empty}\"");

                return;
            }
        }

        Service.Log.Info($"Failed to find the config from \"{arguments}\"!");
    }
}
