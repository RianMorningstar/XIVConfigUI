using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using XIVConfigUI.Attributes;

namespace XIVConfigUI;

/// <summary>
/// 
/// </summary>
public enum FontSize : byte
{
    /// <summary>
    /// 72
    /// </summary>
    First,

    /// <summary>
    /// 48
    /// </summary>
    Second,

    /// <summary>
    /// 32
    /// </summary>
    Third,

    /// <summary>
    /// 24
    /// </summary>
    Forth,

    /// <summary>
    /// 18
    /// </summary>
    Fifth,
}

/// <summary>
/// The class for help.
/// </summary>
public static class ImGuiHelper
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public static ImFontPtr GetFont(FontSize size)
    {
        return GetFont(size switch
        {
            FontSize.First => 72,
            FontSize.Second => 48,
            FontSize.Third => 32,
            FontSize.Forth => 24,
            _ => 18,
        });
    }

    /// <summary>
    /// Get the font based on size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public unsafe static ImFontPtr GetFont(float size)
    {
        var style = new Dalamud.Interface.GameFonts.GameFontStyle(Dalamud.Interface.GameFonts.GameFontStyle.GetRecommendedFamilyAndSize(Dalamud.Interface.GameFonts.GameFontFamily.Axis, size));

        var handle = Service.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(style);

        try
        {
            var font = handle.Lock().ImFont;

            if ((IntPtr)font.NativePtr == IntPtr.Zero)
            {
                return ImGui.GetFont();
            }
            font.Scale = size / font.FontSize;
            return font;
        }
        catch
        {
            return ImGui.GetFont();
        }
    }

    #region PopUp
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="command"></param>
    /// <param name="reset"></param>
    public static void PrepareGroup(string key, string command, Action reset)
    {
        DrawHotKeysPopup(key, command, ("Reset to Default Value.", reset, ["Backspace"]));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="command"></param>
    /// <param name="pairs"></param>
    public static void DrawHotKeysPopup(string key, string command, params (string name, Action action, string[] keys)[] pairs)
    {
        using var popup = ImRaii.Popup(key);
        if (popup)
        {
            if (ImGui.BeginTable(key, 2, ImGuiTableFlags.BordersOuter))
            {
                foreach (var (name, action, keys) in pairs)
                {
                    if (action == null) continue;
                    DrawHotKeys(name, action, keys);
                }
                if (!string.IsNullOrEmpty(command))
                {
                    DrawHotKeys($"Execute \"{command}\"", () => ExecuteCommand(command), "Alt");

                    DrawHotKeys($"Copy \"{command}\"", () => CopyCommand(command), "Ctrl");
                }
                ImGui.EndTable();
            }
        }
    }

    private static void ExecuteCommand(string command)
    {
        Service.Commands.ProcessCommand(command);
    }

    private static void CopyCommand(string command)
    {
        ImGui.SetClipboardText(command);
    }

    private static void DrawHotKeys(string name, Action action, params string[] keys)
    {
        if (action == null) return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if (ImGui.Selectable(name))
        {
            action();
            ImGui.CloseCurrentPopup();
        }

        ImGui.TableNextColumn();
        ImGui.TextDisabled(string.Join(' ', keys));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="command"></param>
    /// <param name="reset"></param>
    /// <param name="showHand"></param>
    public static void ReactPopup(string key, string command, Action reset, bool showHand = true)
    {
        ExecuteHotKeysPopup(key, command, string.Empty, showHand, (reset, new VirtualKey[] { VirtualKey.BACK }));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="command"></param>
    /// <param name="tooltip"></param>
    /// <param name="showHand"></param>
    /// <param name="pairs"></param>
    public static void ExecuteHotKeysPopup(string key, string command, string tooltip, bool showHand, params (Action action, VirtualKey[] keys)[] pairs)
    {
        if (!ImGui.IsItemHovered()) return;
        if (!string.IsNullOrEmpty(tooltip)) ShowTooltip(tooltip);

        if (showHand) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            if (!ImGui.IsPopupOpen(key))
            {
                ImGui.OpenPopup(key);
            }
        }

        foreach (var (action, keys) in pairs)
        {
            if (action == null) continue;
            ExecuteHotKeys(action, keys);
        }
        if (!string.IsNullOrEmpty(command))
        {
            ExecuteHotKeys(() => ExecuteCommand(command), VirtualKey.MENU);
            ExecuteHotKeys(() => CopyCommand(command), VirtualKey.CONTROL);
        }
    }

    private static readonly SortedList<string, bool> _lastChecked = [];
    private static void ExecuteHotKeys(Action action, params VirtualKey[] keys)
    {
        if (action == null) return;
        var name = string.Join(' ', keys);

        if (!_lastChecked.TryGetValue(name, out var last)) last = false;
        var now = keys.All(k => Service.KeyState[k]);
        _lastChecked[name] = now;

        if (!last && now) action();
    }
    #endregion

    #region Image
    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size"></param>
    /// <param name="selected"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public unsafe static bool SilenceImageButton(IntPtr handle, Vector2 size, bool selected, string id = "")
    => SilenceImageButton(handle, size, Vector2.Zero, Vector2.One, selected, id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size"></param>
    /// <param name="uv0"></param>
    /// <param name="uv1"></param>
    /// <param name="selected"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public unsafe static bool SilenceImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, bool selected, string id = "")
    {
        return SilenceImageButton(handle, size, uv0, uv1, selected ? ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.Header)) : 0, id);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size"></param>
    /// <param name="uv0"></param>
    /// <param name="uv1"></param>
    /// <param name="buttonColor"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public unsafe static bool SilenceImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, uint buttonColor, string id = "")
    {
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);

        var result = NoPaddingImageButton(handle, size, uv0, uv1, id);
        ImGui.PopStyleColor(3);

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="wholeWidth"></param>
    /// <param name="maxWidth"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool TextureButton(IDalamudTextureWrap texture, float wholeWidth, float maxWidth, string id = "")
    {
        if (texture == null) return false;

        var size = new Vector2(texture.Width, texture.Height) * MathF.Min(1, MathF.Min(maxWidth, wholeWidth) / texture.Width);

        var result = false;
        DrawItemMiddle(() =>
        {
            result = NoPaddingNoColorImageButton(texture.ImGuiHandle, size, id);
        }, wholeWidth, size.X);
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="drawAction"></param>
    /// <param name="wholeWidth"></param>
    /// <param name="width"></param>
    /// <param name="leftAlign"></param>
    public static void DrawItemMiddle(Action drawAction, float wholeWidth, float width, bool leftAlign = true)
    {
        if (drawAction == null) return;
        var distance = (wholeWidth - width) / 2;
        if (leftAlign) distance = MathF.Max(distance, 0);
        ImGui.SetCursorPosX(distance);
        drawAction();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public unsafe static bool NoPaddingNoColorImageButton(IntPtr handle, Vector2 size, string id = "")
    => NoPaddingNoColorImageButton(handle, size, Vector2.Zero, Vector2.One, id);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size"></param>
    /// <param name="uv0"></param>
    /// <param name="uv1"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public unsafe static bool NoPaddingNoColorImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, string id = "")
    {
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        var result = NoPaddingImageButton(handle, size, uv0, uv1, id);
        ImGui.PopStyleColor(3);

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="size"></param>
    /// <param name="uv0"></param>
    /// <param name="uv1"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool NoPaddingImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, string id = "")
    {
        //TODO maybe push style can make it simple.
        var padding = ImGui.GetStyle().FramePadding;
        ImGui.GetStyle().FramePadding = Vector2.Zero;

        using var pushedId = ImRaii.PushId(id);
        var result = ImGui.ImageButton(handle, size, uv0, uv1);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        ImGui.GetStyle().FramePadding = padding;
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cursor"></param>
    /// <param name="width"></param>
    /// <param name="percent"></param>
    public static void DrawActionOverlay(Vector2 cursor, float width, float percent)
    {
        var pixPerUnit = width / 82;

        if (percent < 0)
        {
            if (XIVConfigUIMain.GetTexture("ui/uld/icona_frame_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 4));

                //var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2((96f * 0 + 4f) / cover.Width, (96f * 2) / cover.Height);

                //Out Size is 88, 96
                //Inner Size is 82, 82
                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
        else if (percent < 1)
        {
            if (XIVConfigUIMain.GetTexture("ui/uld/icona_recast_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 0));

                var P = (int)(percent * 81);


                var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2(P % 9 * step.X, P / 9 * step.Y);

                //Out Size is 88, 96
                //Inner Size is 82, 82
                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
        else
        {
            if (XIVConfigUIMain.GetTexture("ui/uld/icona_frame_hr1.tex", out var cover))
            {

                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 4));

                //Out Size is 88, 96
                //Inner Size is 82, 82
                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    new Vector2(4f / cover.Width, 0f / cover.Height),
                    new Vector2(92f / cover.Width, 94f / cover.Height));
            }
        }

        if (percent > 1)
        {
            if (XIVConfigUIMain.GetTexture("ui/uld/icona_recast2_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 0));

                var P = (int)(percent % 1 * 81);

                var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2((P % 9 + 9) * step.X, P / 9 * step.Y);

                //Out Size is 88, 96
                //Inner Size is 82, 82
                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
    }
    #endregion

    #region Tooltip
    const ImGuiWindowFlags TOOLTIP_FLAG =
      ImGuiWindowFlags.Tooltip |
      ImGuiWindowFlags.NoMove |
      ImGuiWindowFlags.NoSavedSettings |
      ImGuiWindowFlags.NoBringToFrontOnFocus |
      ImGuiWindowFlags.NoDecoration |
      ImGuiWindowFlags.NoInputs |
      ImGuiWindowFlags.AlwaysAutoResize;

    const string TOOLTIP_ID = "Config UI Tooltips";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    public static void HoveredTooltip(string? text)
    {
        if (!ImGui.IsItemHovered()) return;
        ShowTooltip(text);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="act"></param>
    public static void HoveredTooltip(Action act)
    {
        if (!ImGui.IsItemHovered()) return;
        ShowTooltip(act);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    public static void ShowTooltip(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ShowTooltip(() => ImGui.Text(text));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="act"></param>
    public static void ShowTooltip(Action act)
    {
        if (act == null) return;
        if (!XIVConfigUIMain.Config.ShowTooltip) return;

        ImGui.SetNextWindowBgAlpha(1);

        using var color = ImRaii.PushColor(ImGuiCol.BorderShadow, ImGuiColors.DalamudWhite);

        ImGui.SetNextWindowSizeConstraints(new Vector2(150, 0) * ImGuiHelpers.GlobalScale, new Vector2(1200, 1500) * ImGuiHelpers.GlobalScale);
        ImGui.SetWindowPos(TOOLTIP_ID, ImGui.GetIO().MousePos);

        if (ImGui.Begin(TOOLTIP_ID, TOOLTIP_FLAG))
        {
            act();
            ImGui.End();
        }
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="link"></param>
    /// <param name="wholeWidth"></param>
    /// <param name="drawQuestion"></param>
    public static void DrawLinkDescription(LinkDescription link, float wholeWidth, bool drawQuestion)
    {
        var hasTexture = XIVConfigUIMain.GetTexture(link.Url, out var texture);

        if (hasTexture && TextureButton(texture, wholeWidth, wholeWidth))
        {
            Util.OpenLink(link.Url);
        }

        ImGui.TextWrapped(link.Description);

        if (drawQuestion && !hasTexture && !string.IsNullOrEmpty(link.Url))
        {
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Question}##{link.Description}"))
            {
                Util.OpenLink(link.Url);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unit"></param>
    /// <returns></returns>
    public static string ToSymbol(this ConfigUnitType unit) => unit switch
    {
        ConfigUnitType.Seconds => " s",
        ConfigUnitType.Degree => " °",
        ConfigUnitType.Pixels => " p",
        ConfigUnitType.Yalms => " y",
        ConfigUnitType.Percent => " %%",
        _ => string.Empty,
    };
}
