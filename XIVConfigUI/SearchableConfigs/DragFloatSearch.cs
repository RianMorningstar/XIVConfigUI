using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The drag float thing.
/// </summary>
public class DragFloatSearch : Searchable
{
    /// <summary/>
    public RangeAttribute Range { get; }

    /// <summary>
    /// Value.
    /// </summary>
    protected float Value
    {
        get => (float)_property.GetValue(_obj)!;
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
    public DragFloatSearch(PropertyInfo property, object obj) : base(property, obj)
    {
        Range = _property.GetCustomAttribute<RangeAttribute>() ?? new();
    }

    /// <inheritdoc/>
    protected override void DrawMain()
    {
        var value = Value;
        if (ImGuiHelper.DragFloat($"##Config_{ID}{GetHashCode()}", DRAG_WIDTH, ref value, Range))
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
