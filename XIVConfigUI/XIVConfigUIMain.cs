using Dalamud.Interface.Internal;
using Dalamud.Plugin;

namespace XIVConfigUI;
public static class XIVConfigUIMain
{
    private static bool _inited = false;

    internal static SearchableConfig Config { get; private set; } = null!;

    internal static string CommandForChangingSetting { get; private set; } = "/ConfigUI";

    #region GetIcon
    public delegate bool GetTextureFromPath(string path, out IDalamudTextureWrap texture, bool loadingIcon = false);
    public delegate bool GetTextureFromID(uint id, out IDalamudTextureWrap texture, uint @default = 0);

    public static GetTextureFromPath GetTexturePath { get; set; } = GetTextureDefault;
    public static GetTextureFromID GetTextureID { get; set; } = GetTextureDefault;
    internal static bool GetTexture(string path, out IDalamudTextureWrap texture, bool loadingIcon = false)
        => GetTexturePath(path, out texture, loadingIcon);
    internal static bool GetTexture(uint id, out IDalamudTextureWrap texture, uint @default = 0)
        => GetTextureID(id, out texture, @default);

    private static readonly Dictionary<uint, uint> _actionIcons = [];
    internal static bool GetTextureAction(uint id, out IDalamudTextureWrap texture)
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
    private static bool GetTextureDefault(string path, out IDalamudTextureWrap texture, bool loadingIcon = false)
    {
        texture = null!;
        return false;
    }

    private static bool GetTextureDefault(uint id, out IDalamudTextureWrap texture, uint @default = 0)
    {
        texture = null!;
        return false;
    }
    #endregion
    public static void Init(DalamudPluginInterface pluginInterface, string userName, string repoName,
        string commandForChangingSetting, SearchableConfig searchConfig)
    {
        if (_inited) return;
        _inited = true;

        pluginInterface.Create<Service>();
        LocalManager.InIt(userName, repoName);
        CommandForChangingSetting = commandForChangingSetting;
        Config = searchConfig;
    }

    /// <summary>
    /// Don't forget to dispose this!
    /// </summary>
    public static void Dispose()
    {
        if (!_inited) return;
        _inited = false;

        LocalManager.Dispose();
    }
}
