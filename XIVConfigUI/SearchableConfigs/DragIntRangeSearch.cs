using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The drag int.
/// </summary>
public class DragIntRangeSearch : Searchable
{
    /// <summary/>
    public RangeAttribute Range { get; }

    /// <summary>
    /// 
    /// </summary>
    protected Vector2Int Value
    {
        get => (Vector2Int)_property.GetValue(_obj)!;
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
    public DragIntRangeSearch(PropertyInfo property, object obj) : base(property, obj)
    {
        Range = _property.GetCustomAttribute<RangeAttribute>() ?? new();
    }

    /// <inheritdoc/>
    protected override void DrawMain()
    {
        var value = Value;
        ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);
        if (ImGuiHelper.DragInt2($"##Config_{ID}{GetHashCode()}", DRAG_WIDTH, ref value, Range))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();
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
