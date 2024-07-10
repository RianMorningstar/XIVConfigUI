using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using XIVConfigUI.SearchableConfigs;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace XIVConfigUI;

/// <summary>
/// The config window.
/// <inheritdoc/>
/// </summary>
public abstract class ConfigWindow : Window
{
    private string _searchText = string.Empty;

    /// <summary>
    /// The searching result.
    /// </summary>
    protected Searchable[] _searchResults = [];

    /// <summary>
    /// The active tab index.
    /// </summary>
    protected int _activeTabIndex = -1;

    /// <summary>
    /// The scale in the imgui.
    /// </summary>
    protected static float Scale => ImGuiHelpers.GlobalScale;

    /// <summary>
    /// The min column Width in sidebar.
    /// </summary>
    protected static float MinColumnWidth => 24 * Scale;

    /// <summary>
    /// The max icon width in sidebar.
    /// </summary>
    protected static float MaxIconWidth => 50 * Scale;

    /// <summary>
    /// The discord server id.
    /// </summary>
    protected virtual string DiscordServerID => string.Empty;

    /// <summary>
    /// The discord server invite id.
    /// </summary>
    protected virtual string DiscordServerInviteLink => string.Empty;

    /// <summary>
    /// Show the donate link.
    /// </summary>
    protected virtual bool ShowDonate => true;

    /// <summary>
    /// Your kofi page.
    /// </summary>
    protected virtual string Kofi => string.Empty;

    /// <summary>
    /// Your patreon page.
    /// </summary>
    protected virtual string Patreon => string.Empty;

    /// <summary>
    /// Your project name in crowdin.
    /// </summary>
    protected virtual string Crowdin => string.Empty;

    private ConfigWindowItem[]? _items = null;
    private ConfigWindowItem[] Items => _items ??= GetItems();

    /// <summary>
    /// The active item.
    /// </summary>
    protected ConfigWindowItem ActiveItem => Items[_activeTabIndex];

    /// <summary>
    /// The searchable collection.
    /// </summary>
    public virtual SearchableCollection Collection { get; } = new SearchableCollection(new object());

    /// <summary>
    /// The all searchables for searching.
    /// </summary>
    public virtual IEnumerable<Searchable> Searchables => [.. Collection];

    /// <summary>
    /// Creator with assembly name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="flags"></param>
    public ConfigWindow(AssemblyName name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : this((name.Name ?? name.FullName) + " v" + (name.Version?.ToString() ?? "?.?.?"), flags)
    {
    }

    /// <summary>
    /// Create a config window.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="flags"></param>
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

    /// <summary>
    /// Get all items.
    /// </summary>
    /// <returns></returns>
    protected abstract ConfigWindowItem[] GetItems();

    /// <summary>
    /// Clear all items.
    /// </summary>
    public void ClearItems()
    {
        _items = null;
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
        catch (Exception ex)
        {
            Service.Log.Warning(ex, "Something wrong with config window.");
        }
    }

    private void DrawBody()
    {
        ImGui.SetCursorPos(ImGui.GetCursorPos() + (Vector2.One * 8 * Scale));
        using var child = ImRaii.Child("Config Window Body", -Vector2.One);
        if (child)
        {
            if (_searchResults != null && _searchResults.Length != 0)
            {
                using (var font = ImRaii.PushFont(ImGuiHelper.GetFont(FontSize.Fourth)))
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
                    ActiveItem.Draw(this);
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
        DrawDonate();

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
            for (var i = 0; i < Items.Length; i++)
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
                            Invoke();
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, iconSize, 1);
                        if (_activeTabIndex == i)
                        {
                            ImGui.GetWindowDrawList().DrawSlotHighlight(ImGui.GetWindowPos() + cursor, iconSize, 0xfff8cbff);
                        }
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
                        Invoke();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        var desc = item.Description;
                        if (!string.IsNullOrEmpty(desc)) ImGuiHelper.ShowTooltip(desc);
                    }
                }

                void Invoke()
                {
                    if (item.OnClick()) return;

                    if (string.IsNullOrEmpty(item.Link))
                    {
                        _activeTabIndex = i;
                        _searchResults = [];
                    }
                    else
                    {
                        Util.OpenLink(item.Link);
                    }
                }
            }
        }

        void DrawDonate()
        {
            if (!ShowDonate) return;

            float bottom = ImGui.GetWindowSize().Y + ImGui.GetScrollY();
            if (!string.IsNullOrEmpty(Kofi))
            {
                DrawIcon("https://storage.ko-fi.com/cdn/brandasset/kofi_s_logo_nolabel.png",
                    "https://storage.ko-fi.com/cdn/brandasset/kofi_bg_tag_dark.png",
                    "https://ko-fi.com/" + Kofi);
            }

            if (!string.IsNullOrEmpty(Patreon))
            {
                DrawIcon("https://raw.githubusercontent.com/ArchiDog1998/XIVConfigUI/main/Resources/PATREON_SYMBOL_1_WHITE_RGB.png",
                    "https://raw.githubusercontent.com/ArchiDog1998/XIVConfigUI/main/Resources/PATREON_WORDMARK_1_WHITE_RGB.png",
                    "https://www.patreon.com/" + Patreon);
            }

            void DrawIcon(string iconSmall, string iconBig, string link)
            {
                if (wholeWidth <= 60 * Scale
                    ? ImageLoader.GetTexture(iconSmall, out var texture)
                    : ImageLoader.GetTexture(iconBig, out texture))
                {
                    var width = Math.Min(150 * Scale, Math.Max(MinColumnWidth, Math.Min(wholeWidth, texture.Width)));
                    var size = new Vector2(width, width * texture.Height / texture.Width);
                    size *= MathF.Max(MinColumnWidth / size.Y, 1);
                    var result = false;
                    ImGuiHelper.DrawItemMiddle(() =>
                    {
                        ImGui.SetCursorPosY(bottom -= size.Y);
                        result = ImGuiHelper.NoPaddingNoColorImageButton(texture.ImGuiHandle, size, "Donate Plugin");
                    }, wholeWidth, size.X);

                    if (result)
                    {
                        Util.OpenLink(link);
                    }
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

    /// <summary>
    /// Render the header
    /// </summary>
    /// <param name="wholeWidth"></param>
    /// <param name="iconSize"></param>
    protected virtual void DrawHeader(float wholeWidth, float iconSize)
    {
        var size = MathF.Max(MathF.Min(wholeWidth, Scale * 128), MinColumnWidth);

        if (ImageLoader.GetTexture(0, out var overlay))
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

    /// <summary>
    /// The way to get the logo.
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    protected virtual bool GetLogo(out IDalamudTextureWrap texture) 
        => ImageLoader.GetTexture(XIVConfigUIMain.IconUrl, out texture);

    /// <summary>
    /// Draw the sub header.
    /// </summary>
    /// <param name="wholeWidth"></param>
    /// <returns></returns>
    protected virtual bool DrawSubHeader(float wholeWidth) => false;

    /// <summary>
    /// The way to draw the about.
    /// </summary>
    protected virtual void DrawAbout()
    {
        using (var font = ImRaii.PushFont(ImGuiHelper.GetFont(FontSize.Fourth)))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudYellow));
            ImGui.TextWrapped(XIVConfigUIMain.Punchline);
        }

        ImGui.Spacing();

        ImGui.TextWrapped(XIVConfigUIMain.Description);

        var width = ImGui.GetWindowWidth();

        if (!string.IsNullOrEmpty(DiscordServerID)
            && ImageLoader.GetTexture($"https://discordapp.com/api/guilds/{DiscordServerID}/embed.png?style=banner2", out var icon) && ImGuiHelper.TextureButton(icon, width, width))
        {
            if (!string.IsNullOrEmpty(DiscordServerInviteLink))
            {
                Util.OpenLink("https://discord.gg/" + DiscordServerInviteLink);
            }
        }

        if (ImageLoader.GetTexture($"https://GitHub-readme-stats.vercel.app/api/pin/?username={XIVConfigUIMain.UserName}&repo={XIVConfigUIMain.RepoName}&theme=dark", out icon) && ImGuiHelper.TextureButton(icon, width, width))
        {
            Util.OpenLink($"https://GitHub.com/{XIVConfigUIMain.UserName}/{XIVConfigUIMain.RepoName}");
        }
        ImGuiHelper.HoveredTooltip(LocalString.SourceCode.Local());

        if (!string.IsNullOrEmpty(Crowdin)
            && ImageLoader.GetTexture("https://badges.crowdin.net/badge/light/crowdin-on-dark.png", out icon)
            && ImGuiHelper.TextureButton(icon, width, width))
        {
            Util.OpenLink($"https://crowdin.com/project/{Crowdin}");
        }
        ImGuiHelper.HoveredTooltip(LocalString.Localization.Local());
    }
}
