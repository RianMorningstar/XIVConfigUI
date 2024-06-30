using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using XIVConfigUI.Attributes;
using XIVConfigUI.SearchableConfigs;

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
    Fourth,

    /// <summary>
    /// 18
    /// </summary>
    Fifth,

    /// <summary>
    /// 12
    /// </summary>
    Sixth,
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
    /// <param name="fontFamily"></param>
    /// <returns></returns>
    public static ImFontPtr GetFont(FontSize size, GameFontFamily fontFamily = GameFontFamily.Axis)
    {
        return GetFont(size switch
        {
            FontSize.First => 72,
            FontSize.Second => 48,
            FontSize.Third => 32,
            FontSize.Fourth => 24,
            FontSize.Fifth => 18,
            _ => 12,
        }, fontFamily);
    }

    /// <summary>
    /// Get the font based on size.
    /// </summary>
    /// <param name="size"></param>
    /// <param name="fontFamily"></param>
    /// <returns></returns>
    public static unsafe ImFontPtr GetFont(float size, GameFontFamily fontFamily = GameFontFamily.Axis)
    {
        var style = new GameFontStyle(GameFontStyle.GetRecommendedFamilyAndSize(fontFamily, size));

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
        DrawHotKeysPopup(key, command, (LocalString.ResetToDefault.Local(), reset, ["Backspace"]));
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
                    DrawHotKeys(string.Format(LocalString.ExecuteCommand.Local(), command), 
                        () => ExecuteCommand(command), "Alt");

                    DrawHotKeys(string.Format(LocalString.CopyCommand.Local(), command), 
                        () => CopyCommand(command), "Ctrl");
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
    public static unsafe bool SilenceImageButton(IntPtr handle, Vector2 size, bool selected, string id = "")
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
    public static unsafe bool SilenceImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, bool selected, string id = "")
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
    public static unsafe bool SilenceImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, uint buttonColor, string id = "")
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
    public static unsafe bool NoPaddingNoColorImageButton(IntPtr handle, Vector2 size, string id = "")
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
    public static unsafe bool NoPaddingNoColorImageButton(IntPtr handle, Vector2 size, Vector2 uv0, Vector2 uv1, string id = "")
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
            if (ImageLoader.GetTexture("ui/uld/icona_frame_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 4));

                //var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2(((96f * 0) + 4f) / cover.Width, (96f * 2) / cover.Height);

                //Out Size is 88, 96
                //Inner Size is 82, 82
                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
        else if (percent < 1)
        {
            if (ImageLoader.GetTexture("ui/uld/icona_recast_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 0));

                var P = (int)(percent * 81);

                var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2((P % 9) * step.X, P / 9 * step.Y);

                //Out Size is 88, 96
                //Inner Size is 82, 82
                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
        else
        {
            if (ImageLoader.GetTexture("ui/uld/icona_frame_hr1.tex", out var cover))
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
            if (ImageLoader.GetTexture("ui/uld/icona_recast2_hr1.tex", out var cover))
            {
                ImGui.SetCursorPos(cursor - new Vector2(pixPerUnit * 3, pixPerUnit * 0));

                var P = (int)((percent % 1) * 81);

                var step = new Vector2(88f / cover.Width, 96f / cover.Height);
                var start = new Vector2(((P % 9) + 9) * step.X, P / 9 * step.Y);

                //Out Size is 88, 96
                //Inner Size is 82, 82
                ImGui.Image(cover.ImGuiHandle, new Vector2(pixPerUnit * 88, pixPerUnit * 94),
                    start, start + new Vector2(88f / cover.Width, 94f / cover.Height));
            }
        }
    }
    #endregion

    #region Tooltip
    private const ImGuiWindowFlags TOOLTIP_FLAG =
      ImGuiWindowFlags.Tooltip |
      ImGuiWindowFlags.NoMove |
      ImGuiWindowFlags.NoSavedSettings |
      ImGuiWindowFlags.NoBringToFrontOnFocus |
      ImGuiWindowFlags.NoDecoration |
      ImGuiWindowFlags.NoInputs |
      ImGuiWindowFlags.AlwaysAutoResize;

    private const string TOOLTIP_ID = "Config UI Tooltips";

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
        if (!XIVConfigUIMain.ShowTooltip()) return;

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
        var hasTexture = ImageLoader.GetTexture(link.Url, out var texture);

        if (hasTexture && TextureButton(texture, wholeWidth, wholeWidth))
        {
            Util.OpenLink(link.Url);
        }

        if (!string.IsNullOrEmpty(link.Description))
        {
            ImGui.TextWrapped(link.Description);
        }

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

    private static string _searchKey = string.Empty;
    /// <summary>
    /// Selectable combo.
    /// </summary>
    /// <param name="popUp"></param>
    /// <param name="items"></param>
    /// <param name="index"></param>
    /// <param name="font"></param>
    /// <param name="color"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public static unsafe bool SelectableCombo(string popUp, string[] items, ref int index, ImFontPtr? font = null, Vector4? color = null, string description = "")
    {
        var count = items.Length;

        if (count == 0)
        {
            ImGui.TextWrapped(LocalString.Nothing.Local());
            return false;
        }

        var originIndex = index;
        index = Math.Max(0, index) % count;
        var name = items[index] + "##" + popUp;

        var result = originIndex != index;

        if (SelectableButton(name, font, color))
        {
            if (count < 3)
            {
                index = (index + 1) % count;
                result = true;
            }
            else
            {
                if (!ImGui.IsPopupOpen(popUp)) ImGui.OpenPopup(popUp);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (!string.IsNullOrEmpty(description))
            {
                ShowTooltip(description);
            }
        }

        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, Vector2.One * 500 * ImGuiHelpers.GlobalScale);

        if (ImGui.BeginPopup(popUp))
        {
            List<(int, string)> pairs = [];
            for (int i = 0; i < count; i++)
            {
                pairs.Add((i, items[i]));
            }
            var members = pairs.OrderByDescending(s => Searchable.Similarity(s.Item2, _searchKey));

            ImGui.SetNextItemWidth(Math.Max(ImGuiHelpers.GetButtonSize(LocalString.Searching.Local()).X, 
                members.Max(i => ImGuiHelpers.GetButtonSize(i.Item2).X) + ImGui.GetStyle().ScrollbarSize));
            ImGui.InputTextWithHint("##Searching the member", LocalString.Searching.Local(), ref _searchKey, 128);

            ImGui.Spacing();

            ImRaii.IEndObject? child = null;
            if (members.Count() >= 15)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(0, 300) * ImGuiHelpers.GlobalScale, new Vector2(500, 300) * ImGuiHelpers.GlobalScale);
                child = ImRaii.Child(popUp + "Child");
                if (!child) return result;
            }

            foreach (var member in members)
            {
                if (ImGui.Selectable(member.Item2))
                {
                    index = member.Item1;
                    result = true;
                    ImGui.CloseCurrentPopup();
                }
            }

            child?.Dispose();
            ImGui.EndPopup();
        }

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="font"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static unsafe bool SelectableButton(string name, ImFontPtr? font = null, Vector4? color = null)
    {
        List<IDisposable> disposables = new(2);
        if (font != null)
        {
            disposables.Add(ImRaii.PushFont(font.Value));
        }
        if (color != null)
        {
            disposables.Add(ImRaii.PushColor(ImGuiCol.Text, color.Value));
        }
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderActive)));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(*ImGui.GetStyleColorVec4(ImGuiCol.HeaderHovered)));
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        var result = ImGui.Button(name);
        ImGui.PopStyleColor(3);
        foreach (var item in disposables)
        {
            item.Dispose();
        }

        return result;
    }

    /// <summary>
    /// Drag Float.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="width"></param>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool DragFloat(string name, float width, ref float value, RangeAttribute range)
    {
        var show = range.UnitType == ConfigUnitType.Percent ? $"{value * 100:F1}{range.UnitType.ToSymbol()}" : $"{value:F2}{range.UnitType.ToSymbol()}";

        ImGui.SetNextItemWidth(Math.Max(width * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(show).X + 10 * ImGuiHelpers.GlobalScale));

        if (range.UnitType == ConfigUnitType.Percent)
        {
            if (ImGui.SliderFloat(name, ref value, range.MinValue, range.MaxValue, show))
            {
                value = float.Round(value, 3);
                return true;
            }
        }
        else
        {
            if (ImGui.DragFloat(name, ref value, range.Speed, range.MinValue, range.MaxValue, show))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="width"></param>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool DragInt(string name, float width, ref int value, RangeAttribute range)
    {
        var show = $"{value}{range.UnitType.ToSymbol()}";
        ImGui.SetNextItemWidth(Math.Max(width * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(show).X + 10 * ImGuiHelpers.GlobalScale));

        if (ImGui.DragInt(name, ref value, range.Speed, (int)range.MinValue, (int)range.MaxValue, show))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="width"></param>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool DragFloat2(string name, float width, ref Vector2 value, RangeAttribute range)
    {
        var showMin = range.UnitType == ConfigUnitType.Percent ? $"{value.X * 100:F1}{range.UnitType.ToSymbol()}" : $"{value.X:F2}{range.UnitType.ToSymbol()}";
        var showMax = range.UnitType == ConfigUnitType.Percent ? $"{value.Y * 100:F1}{range.UnitType.ToSymbol()}" : $"{value.Y:F2}{range.UnitType.ToSymbol()}";

        ImGui.SetNextItemWidth(Math.Max(width * ImGuiHelpers.GlobalScale, 
            Math.Max(ImGui.CalcTextSize(showMin).X, ImGui.CalcTextSize(showMax).X) * 2 
            + 20 * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X));

        if (ImGui.DragFloatRange2(name, ref value.X, ref value.Y, range.Speed, range.MinValue, range.MaxValue, showMin, showMax))
        {
            value.X = Math.Min(value.X, value.Y);
            value.Y = Math.Max(value.X, value.Y);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="width"></param>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool DragInt2(string name, float width, ref Vector2Int value, RangeAttribute range)
    {
        var showMin =$"{value.X}{range.UnitType.ToSymbol()}";
        var showMax =$"{value.Y}{range.UnitType.ToSymbol()}";

        ImGui.SetNextItemWidth(Math.Max(width * ImGuiHelpers.GlobalScale,
            Math.Max(ImGui.CalcTextSize(showMin).X, ImGui.CalcTextSize(showMax).X) * 2
            + 20 * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X));

        if (ImGui.DragIntRange2(name, ref value.X, ref value.Y, range.Speed, (int)range.MinValue, (int)range.MaxValue, showMin, showMax))
        {
            value.X = Math.Min(value.X, value.Y);
            value.Y = Math.Max(value.X, value.Y);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="width"></param>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public static bool DragFloat4(string name, float width, ref Vector4 value, RangeAttribute range)
    {
        using var grp = ImRaii.Group();
        var result = false;

        if (DragFloat("L" + name, width, ref value.X, range))
        {
            result = true;
        }
        ImGui.SameLine();

        if (DragFloat("T" + name, width, ref value.Y, range))
        {
            result = true;
        }
        ImGui.SameLine();

        if (DragFloat("R" + name, width, ref value.Z, range))
        {
            result = true;
        }
        ImGui.SameLine();

        if (DragFloat("B" + name, width, ref value.W, range))
        {
            result = true;
        }

        return result;
    }

    /// <summary>
    /// Set the next item with the string length.
    /// </summary>
    /// <param name="name"></param>
    public static void SetNextWidthWithName(string name)
    {
        ImGui.SetNextItemWidth(Math.Max(80 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(name).X + 30 * ImGuiHelpers.GlobalScale));
    }

    /// <summary>
    /// Get cleaned named enu.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Enum[] GetCleanedEnumValues(this Type type)
    {
        if (!type.IsEnum) return [];
        return Enum.GetValues(type).Cast<Enum>().Where(i => i.GetAttribute<ObsoleteAttribute>() == null).ToHashSet().ToArray();
    }
}
