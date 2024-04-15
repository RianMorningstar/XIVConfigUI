using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The searchable item
/// </summary>
public abstract class Searchable
{
    internal readonly SearchableConfig? _config;
    /// <summary/>
    public readonly object _obj;

    /// <summary>
    /// 
    /// </summary>
    public object _default;

    /// <summary>
    /// The property.
    /// </summary>
    public readonly PropertyInfo _property;

    /// <summary>
    /// The width of the drag.
    /// </summary>
    public const float DRAG_WIDTH = 150;

    /// <summary>
    /// The scale of it.
    /// </summary>
    protected static float Scale => ImGuiHelpers.GlobalScale;

    /// <summary>
    /// The parent item.
    /// </summary>
    public CheckBoxSearch? Parent { get; set; } = null;

    /// <summary>
    /// The way to search it.
    /// </summary>
    public virtual string SearchingKeys => Name + " " + Description;

    /// <summary>
    /// The name of it.
    /// </summary>
    public virtual string Name => _property.LocalUIName();

    /// <summary>
    /// The description about this item.
    /// </summary>
    public virtual string Description => _property.LocalUIDescription();

    internal string LeadingCommand
    {
        get
        {
            var type = _obj.GetType();
            var command = type.GetCustomAttribute<CommandAttribute>()?.SubCommand ?? type.Name;
            return command + " " + _property.Name;
        }
    }

    /// <summary>
    /// The command of this item.
    /// </summary>
    public virtual string Command
    {
        get
        {
            var result = XIVConfigUIMain.Command + " " + LeadingCommand;
            var extra = _default?.ToString();
            if (!string.IsNullOrEmpty(extra)) result += " " + extra;
            return result;
        }
    }

    /// <summary>
    /// The tooltips of it.
    /// </summary>
    public LinkDescription[]? Tooltips => [.. _property.GetCustomAttributes<LinkDescriptionAttribute>().Select(l => l.LinkDescription)];

    /// <summary>
    /// The string id of this item.
    /// </summary>
    public virtual string ID => _property.Name;
    private string Popup_Key => "ConfigUI RightClicking: " + ID;

    /// <summary>
    /// The color of this item.
    /// </summary>
    public uint Color { get; set; } = 0;

    /// <summary>
    /// Should this be shown in the child.
    /// </summary>
    public virtual bool ShowInChild => true;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    /// <param name="obj"></param>
    protected Searchable(PropertyInfo property, object obj)
    {
        _config = SearchableCollection._searchableConfig;
        _property = property;
        _obj = obj;
        if (_config?.GeneratDefault ?? false)
        {
            _default = property.GetValue(Activator.CreateInstance(obj.GetType()))!;
        }
        else
        {
            _default = null!;
        }
    }

    /// <summary>
    /// To draw it.
    /// </summary>
    public unsafe void Draw()
    {
        if (_config?.IsPropertyValid(_property) ?? true)
        {
            DrawMain();
            ImGuiHelper.PrepareGroup(Popup_Key, Command, () => ResetToDefault());
        }
        else
        {
            var textColor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);

            ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));

            var cursor = ImGui.GetCursorPos() + ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
            ImGui.TextWrapped(Name);
            ImGui.PopStyleColor();

            var step = ImGui.CalcTextSize(Name);
            var size = ImGui.GetItemRectSize();
            var height = step.Y / 2;
            var wholeWidth = step.X;
            while (height < size.Y)
            {
                var pt = cursor + new Vector2(0, height);
                ImGui.GetWindowDrawList().AddLine(pt, pt + new Vector2(Math.Min(wholeWidth, size.X), 0), ImGui.ColorConvertFloat4ToU32(textColor));
                height += step.Y;
                wholeWidth -= size.X;
            }

            ImGuiHelper.HoveredTooltip(() => _config?.PropertyInvalidTooltip(_property));
            return;
        }
    }

    /// <summary>
    /// The way to draw it.
    /// </summary>
    protected abstract void DrawMain();

    /// <summary>
    /// To show the tool tips.
    /// </summary>
    /// <param name="showHand"></param>
    protected void ShowTooltip(bool showHand = true)
    {
        var showDesc = !string.IsNullOrEmpty(Description);
        if (showDesc || Tooltips != null && Tooltips.Length > 0)
        {
            ImGuiHelper.ShowTooltip(() =>
            {
                if (showDesc)
                {
                    ImGui.TextWrapped(Description);
                }
                if (showDesc && Tooltips != null && Tooltips.Length > 0)
                {
                    ImGui.Separator();
                }
                var wholeWidth = ImGui.GetWindowWidth();

                if (Tooltips != null)
                {
                    foreach (var tooltip in Tooltips)
                    {
                        ImGuiHelper.DrawLinkDescription(tooltip, wholeWidth, false);
                    }
                }

                TooltipAdditional();
            });
        }

        ImGuiHelper.ReactPopup(Popup_Key, Command, ResetToDefault, showHand);
    }

    /// <summary>
    /// The extra tooltips.
    /// </summary>
    protected virtual void TooltipAdditional() { }

    /// <summary>
    /// The way to reset to the default.
    /// </summary>
    public virtual void ResetToDefault()
    {
        _property.SetValue(_obj, _default);
    }

    /// <summary>
    /// Draw the name of it.
    /// </summary>
    protected void DrawName()
    {
        using (var group = ImRaii.Group())
        {
            _config?.PreNameDrawing(_property);
            ImGui.SameLine();
            if (Color != 0) ImGui.PushStyleColor(ImGuiCol.Text, Color);
            ImGui.TextWrapped(Name);
            if (Color != 0) ImGui.PopStyleColor();
        }

        if (ImGui.IsItemHovered()) ShowTooltip(false);
    }

    /// <summary>
    /// On the string value to change this value.
    /// </summary>
    /// <param name="value"></param>
    public virtual void OnCommand(string value)
    {
        var type = _property.PropertyType;
        object? v = null;

        if (!string.IsNullOrEmpty(value))
        {
            try
            {
                v = Convert.ChangeType(value, type);
            }
            catch (Exception e)
            {
                v = null;
                Service.Log.Debug(e, $"Faild to convert the \"{value}\" to the type {type.FullName} in {this.GetType().FullName}");
            }
        }

        if (v != null)
        {
            _property.SetValue(_obj, v);
            return;
        }

        if (type == typeof(bool)) //Toggle.
        {
            _property.SetValue(_obj,!(bool)_property.GetValue(_obj)!);
            return;
        }
    }

    internal static Searchable[] SimilarItems(IEnumerable<Searchable> items, string searchingText)
    {
        if (string.IsNullOrEmpty(searchingText)) return [];

        const int MAX_RESULT_LENGTH = 20;

        var results = new Searchable[MAX_RESULT_LENGTH];

        var enumerator = items
            .OrderByDescending(i => Similarity(i.SearchingKeys, searchingText))
            .Select(GetParent).GetEnumerator();

        int index = 0;
        while (enumerator.MoveNext() && index < MAX_RESULT_LENGTH)
        {
            if (results.Contains(enumerator.Current)) continue;
            results[index++] = enumerator.Current;
        }
        return results;

        static Searchable GetParent(Searchable searchable)
        {
            if (searchable.Parent == null) return searchable;
            return GetParent(searchable.Parent);
        }
    }

    private static readonly char[] _splitChar = [' ', ',', '、', '.', '。'];


    /// <summary>
    /// The similarity of the two texts.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static float Similarity(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        var chars = text.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);
        var keys = key.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);

        var startWithCount = chars.Count(i => keys.Any(k => i.StartsWith(k, StringComparison.OrdinalIgnoreCase)));

        var containCount = chars.Count(i => keys.Any(k => i.Contains(k, StringComparison.OrdinalIgnoreCase)));

        return startWithCount * 3 + containCount;
    }
}
