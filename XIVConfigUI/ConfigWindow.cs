using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using XIVConfigUI.SearchableConfigs;
using Dalamud.Interface.Internal;

namespace XIVConfigUI;

/// <summary>
/// The config window.
/// <inheritdoc/>
/// </summary>
public abstract class ConfigWindow : Window
{
    private string _searchText = string.Empty;
    protected Searchable[] _searchResults = [];
    protected int _activeTabIndex = -1;

    protected static float Scale => ImGuiHelpers.GlobalScale;

    protected static float MinColumnWidth => 24 * Scale;
    protected static float MaxIconWidth => 50 * Scale;

    /// <summary>
    /// Your kofi-icon.
    /// </summary>
    protected virtual string Kofi => string.Empty;

    private ConfigWindowItem[]? _items = null;
    protected ConfigWindowItem[] Items => _items ??= GetItems();
    public abstract SearchableCollection AllSearchable { get; }

    public virtual IEnumerable<Searchable> Searchables => AllSearchable;

    public ConfigWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, flags | ImGuiWindowFlags.NoScrollbar, false)
    {
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 300),
            MaximumSize = new Vector2(5000, 5000),
        };
        RespectCloseHotkey = true;
    }

    protected virtual ConfigWindowItem[] GetItems()
    {
        return GetType().GetNestedTypes()
            .Where(t => t.IsAssignableTo(typeof(ConfigWindowItem)) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(Activator.CreateInstance).OfType<ConfigWindowItem>().ToArray();
    }

    /// <inheritdoc/>
    public sealed override void Draw()
    {
        using var id = ImRaii.PushId(GetHashCode().ToString());
        using var style = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
        try
        {
            var leftTop = ImGui.GetWindowPos() + ImGui.GetCursorPos();
            var rightDown = leftTop + ImGui.GetWindowSize();
            var screenSize = ImGuiHelpers.MainViewport.Size;
            if ((leftTop.X <= 0 || leftTop.Y <= 0 || rightDown.X >= screenSize.X || rightDown.Y >= screenSize.Y)
                && !ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                var str = string.Empty;
                for (int i = 0; i < 150; i++)
                {
                    str += "Move away! Don't crash! ";
                }

                using var font = ImRaii.PushFont(ImGuiHelper.GetFont(24));
                using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
                ImGui.TextWrapped(str);
            }
            else
            {
                using var table = ImRaii.Table("Rotation Config Table", 2, ImGuiTableFlags.Resizable);
                if (table)
                {
                    ImGui.TableSetupColumn("Rotation Config Side Bar", ImGuiTableColumnFlags.WidthFixed, 100 * Scale);
                    ImGui.TableNextColumn();

                    try
                    {
                        DrawSideBar();
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Warning(ex, "Something wrong with sideBar");
                    }

                    ImGui.TableNextColumn();

                    try
                    {
                        DrawBody();
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Warning(ex, "Something wrong with body");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Something wrong with config window.");
        }
    }

    private void DrawBody()
    {
        ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.One * 8 * Scale);
        using var child = ImRaii.Child("Config Window Body", -Vector2.One);
        if (child)
        {
            if (_searchResults != null && _searchResults.Length != 0)
            {
                using (var font = ImRaii.PushFont(ImGuiHelper.GetFont(FontSize.Forth)))
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
                    ImGui.TextWrapped(LocalString.Search_Result.Local());
                }

                ImGui.Spacing();

                foreach (var searchable in _searchResults)
                {
                    searchable?.Draw();
                }
            }
            else
            {
                if (_activeTabIndex >= Items.Length)
                {
                    _activeTabIndex = -1;
                }
                if (_activeTabIndex < 0)
                {
                    DrawAbout();
                }
                else
                {
                    Items[_activeTabIndex].Draw(this);
                }
            }
        }
    }

    private void DrawSideBar()
    {
        using var child = ImRaii.Child("Config Window Side bar", -Vector2.One, false, ImGuiWindowFlags.NoScrollbar);
        if (!child) return;

        var wholeWidth = ImGui.GetWindowSize().X;
        var iconSize = Math.Max(MinColumnWidth, Math.Min(wholeWidth, MaxIconWidth));
        DrawHeader(wholeWidth, iconSize);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        iconSize *= 0.6f;

        DrawSearchingBox();
        DrawList();
        DrawKofi();

        void DrawSearchingBox()
        {
            if (wholeWidth > MaxIconWidth)
            {
                if (DrawSubHeader(wholeWidth))
                {
                    ImGui.Separator();
                    ImGui.Spacing();
                }

                ImGui.SetNextItemWidth(wholeWidth);
                SearchingBox();

                ImGui.Spacing();
            }
            else
            {
                if (ImageLoader.GetTexture(46, out var icon))
                {
                    ImGuiHelper.DrawItemMiddle(() =>
                    {
                        using var popup = ImRaii.Popup("Searching Popup");
                        if (popup)
                        {
                            ImGui.SetNextItemWidth(200 * Scale);
                            SearchingBox();
                            if (ImGui.IsKeyDown(ImGuiKey.Enter))
                            {
                                ImGui.CloseCurrentPopup();
                            }
                        }

                        var cursor = ImGui.GetCursorPos();
                        if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * iconSize))
                        {
                            ImGui.OpenPopup("Searching Popup");
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, iconSize, -1);
                        ImGuiHelper.HoveredTooltip(LocalString.Search.Local());

                    }, Math.Max(MinColumnWidth, wholeWidth), iconSize);
                }
            }
        }

        void DrawList()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i];

                if (item.IsSkip) continue;

                if (item.GetIcon(out var icon) && wholeWidth <= MaxIconWidth)
                {
                    ImGuiHelper.DrawItemMiddle(() =>
                    {
                        var cursor = ImGui.GetCursorPos();
                        if (ImGuiHelper.NoPaddingNoColorImageButton(icon.ImGuiHandle, Vector2.One * iconSize, item.GetHashCode().ToString()))
                        {
                            _activeTabIndex = i;
                            _searchResults = [];
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, iconSize, _activeTabIndex == i ? 1 : 0);
                    }, Math.Max(MinColumnWidth, wholeWidth), iconSize);

                    var desc = item.Name;
                    var addition = item.Description;
                    if (!string.IsNullOrEmpty(addition)) desc += "\n \n" + addition;
                    ImGuiHelper.HoveredTooltip(desc);
                }
                else
                {
                    if (ImGui.Selectable(item.Name, _activeTabIndex == i, ImGuiSelectableFlags.None, new Vector2(0, 20)))
                    {
                        _activeTabIndex = i;
                        _searchResults = [];
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        var desc = item.Description;
                        if (!string.IsNullOrEmpty(desc)) ImGuiHelper.ShowTooltip(desc);
                    }
                }
            }
        }

        void DrawKofi()
        {
            if (string.IsNullOrEmpty(Kofi)) return;

            if (wholeWidth <= 60 * Scale
                ? ImageLoader.GetTexture("https://storage.ko-fi.com/cdn/brandasset/kofi_s_logo_nolabel.png", out var texture)
                : ImageLoader.GetTexture("https://storage.ko-fi.com/cdn/brandasset/kofi_bg_tag_dark.png", out texture))
            {
                var width = Math.Min(150 * Scale, Math.Max(MinColumnWidth, Math.Min(wholeWidth, texture.Width)));
                var size = new Vector2(width, width * texture.Height / texture.Width);
                size *= MathF.Max(MinColumnWidth / size.Y, 1);
                var result = false;
                ImGuiHelper.DrawItemMiddle(() =>
                {
                    ImGui.SetCursorPosY(ImGui.GetWindowSize().Y + ImGui.GetScrollY() - size.Y);
                    result = ImGuiHelper.NoPaddingNoColorImageButton(texture.ImGuiHandle, size, "Donate Plugin");
                }, wholeWidth, size.X);

                if (result)
                {
                    Util.OpenLink("https://ko-fi.com/" + Kofi);
                }
            }
        }

        void SearchingBox()
        {
            if (ImGui.InputTextWithHint("##Config UI Search Box", LocalString.Searching.Local(), ref _searchText, 128, ImGuiInputTextFlags.AutoSelectAll))
            {
                _searchResults = Searchable.SimilarItems(Searchables, _searchText);
            }
        }
    }

    protected virtual void DrawHeader(float wholeWidth, float iconSize)
    {
        var size = MathF.Max(MathF.Min(wholeWidth, Scale * 128), MinColumnWidth);

        if (ImageLoader.GetTexture((uint)0, out var overlay))
        {
            ImGuiHelper.DrawItemMiddle(() =>
            {
                var cursor = ImGui.GetCursorPos();
                if (ImGuiHelper.SilenceImageButton(overlay.ImGuiHandle, Vector2.One * size,
                    _activeTabIndex == -1, "About Icon"))
                {
                    _activeTabIndex = -1;
                    _searchResults = [];
                }
                ImGuiHelper.HoveredTooltip(XIVConfigUIMain.Punchline);

                if (GetLogo(out var logo))
                {
                    ImGui.SetCursorPos(cursor);
                    ImGui.Image(logo.ImGuiHandle, Vector2.One * size);
                }
            }, wholeWidth, size);

            ImGui.Spacing();
        }
    }

    protected virtual bool GetLogo(out IDalamudTextureWrap texture) 
        => ImageLoader.GetTexture(XIVConfigUIMain.IconUrl, out texture);

    protected abstract bool DrawSubHeader(float wholeWidth);

    protected virtual void DrawAbout()
    {
        using (var font = ImRaii.PushFont(ImGuiHelper.GetFont(FontSize.Forth)))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
            ImGui.TextWrapped(XIVConfigUIMain.Punchline);
        }

        ImGui.Spacing();

        ImGui.TextWrapped(XIVConfigUIMain.Description);
    }
}
