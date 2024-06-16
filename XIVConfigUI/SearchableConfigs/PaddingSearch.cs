using Dalamud.Interface.Utility.Raii;
using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The padding search.
/// </summary>
public class PaddingSearch: Searchable
{
    /// <summary/>
    public RangeAttribute Range { get; }

    /// <summary>
    /// Value
    /// </summary>
    protected Vector4 Value
    {
        get => (Vector4)_property.GetValue(_obj)!;
        set
        {
            _property.SetValue(_obj, value);
            _config?.AfterConfigChange(this);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    /// <param name="obj"></param>
    public PaddingSearch(PropertyInfo property, object obj) : base(property, obj)
    {
        Range = _property.GetCustomAttribute<RangeAttribute>() ?? new();
    }

    /// <inheritdoc/>
    protected override void DrawMain()
    {
        var value = Value;

        if (ImGuiHelper.DragFloat4($"##Config_{ID}{GetHashCode()}", DRAG_WIDTH, ref value, Range))
        {
            Value = value;
        }

        if (ImGui.IsItemHovered()) ShowTooltip();
        ImGui.SameLine();

        DrawName();
    }

    /// <inheritdoc/>
    protected override void TooltipAdditional()
    {
        ImGui.Separator();
        ImGui.TextDisabled(Range.UnitType.Local());
        base.TooltipAdditional();
    }
}
