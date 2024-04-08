using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The searchable item
/// </summary>
/// <param name="property">The property to link with.</param>
/// <param name="obj">the config</param>
public abstract class Searchable(PropertyInfo property, object obj)
{
    /// <summary/>
    protected readonly object _obj = obj,
        _default = property.GetValue(Activator.CreateInstance(obj.GetType()))!;

    /// <summary>
    /// The property.
    /// </summary>
    protected readonly PropertyInfo _property = property;

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
    public virtual string Name
    {
        get
        {
            var ui = _property.GetCustomAttribute<UIAttribute>();
            if (ui == null) return string.Empty;

            return _property.Local("Name", ui.Name);
        }
    }

    /// <summary>
    /// The description about this item.
    /// </summary>
    public virtual string Description
    {
        get
        {
            var ui = _property.GetCustomAttribute<UIAttribute>();
            if (ui == null || string.IsNullOrEmpty(ui.Description)) return string.Empty;

            return _property.Local("Description", ui.Description);
        }
    }

    /// <summary>
    /// The command of this item.
    /// </summary>
    public virtual string Command
    {
        get
        {
            var result = XIVConfigUIMain.CommandForChangingSetting + " " + _property.Name;
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
    /// To draw it.
    /// </summary>
    public unsafe void Draw()
    {
        if (XIVConfigUIMain.Config.IsPropertyValid(_property))
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

            ImGuiHelper.HoveredTooltip(() => XIVConfigUIMain.Config.PropertyInvalidTooltip(_property));
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
            XIVConfigUIMain.Config.PreNameDrawing(_property);
            ImGui.SameLine();
            if (Color != 0) ImGui.PushStyleColor(ImGuiCol.Text, Color);
            ImGui.TextWrapped(Name);
            if (Color != 0) ImGui.PopStyleColor();
        }

        if (ImGui.IsItemHovered()) ShowTooltip(false);
    }
}
