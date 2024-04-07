using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.Havok;
using System.Numerics;
using System.Reflection;
using XIVConfigUI;

namespace XIVConfigUI.SearchableConfigs;

internal abstract class Searchable(PropertyInfo property, object obj)
{
    protected readonly object _obj = obj,
        _default = property.GetValue(Activator.CreateInstance(obj.GetType()))!;

    protected readonly PropertyInfo _property = property;

    public const float DRAG_WIDTH = 150;
    protected static float Scale => ImGuiHelpers.GlobalScale;
    public CheckBoxSearch? Parent { get; set; } = null;

    public virtual string SearchingKeys => Name + " " + Description;
    public virtual string Name
    {
        get
        {
            var ui = _property.GetCustomAttribute<UIAttribute>();
            if (ui == null) return string.Empty;

            return _property.Local("Name", ui.Name);
        }
    }
    public virtual string Description
    {
        get
        {
            var ui = _property.GetCustomAttribute<UIAttribute>();
            if (ui == null || string.IsNullOrEmpty(ui.Description)) return string.Empty;

            return _property.Local("Description", ui.Description);
        }
    }
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
    public virtual LinkDescription[]? Tooltips => [.. _property.GetCustomAttributes<LinkDescriptionAttribute>().Select(l => l.LinkDescription)];
    public virtual string ID => _property.Name;
    private string Popup_Key => "ConfigUI RightClicking: " + ID;
    public uint Color { get; set; } = 0;

    public virtual bool ShowInChild => true;

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

    protected abstract void DrawMain();

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

    protected virtual void TooltipAdditional() { }

    public void ResetToDefault()
    {
        _property.SetValue(_obj, _default);
    }

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
