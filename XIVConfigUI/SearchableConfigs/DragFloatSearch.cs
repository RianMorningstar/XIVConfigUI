using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The drag float thing.
/// </summary>
public class DragFloatSearch : Searchable
{
    /// <summary>
    /// 
    /// </summary>
    public float Min { get; }

    /// <summary>
    /// 
    /// </summary>
    public float Max { get; }

    /// <summary>
    /// 
    /// </summary>
    public float Speed { get; }

    /// <summary>
    /// 
    /// </summary>
    public ConfigUnitType Unit { get; }

    /// <summary>
    /// Value.
    /// </summary>
    protected float Value
    {
        get => (float)_property.GetValue(_obj)!;
        set => _property.SetValue(_obj, value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    /// <param name="obj"></param>
    public DragFloatSearch(PropertyInfo property, object obj) : base(property, obj)
    {
        var range = _property.GetCustomAttribute<RangeAttribute>();
        Min = range?.MinValue ?? 0f;
        Max = range?.MaxValue ?? 1f;
        Speed = range?.Speed ?? 0.001f;
        Unit = range?.UnitType ?? ConfigUnitType.None;
    }

    /// <inheritdoc/>
    protected override void DrawMain()
    {
        var value = Value;
        ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);

        if (Unit == ConfigUnitType.Percent)
        {
            if (ImGui.SliderFloat($"##Config_{ID}{GetHashCode()}", ref value, Min, Max, $"{value * 100f:F1}{Unit.ToSymbol()}"))
            {
                Value = float.Round(value, 3);
            }
        }
        else
        {
            if (ImGui.DragFloat($"##Config_{ID}{GetHashCode()}", ref value, Speed, Min, Max, $"{value:F2}{Unit.ToSymbol()}"))
            {
                Value = value;
            }
        }

        if (ImGui.IsItemHovered()) ShowTooltip();

        DrawName();
    }

    /// <inheritdoc/>
    protected override void TooltipAdditional()
    {
        ImGui.Separator();
        ImGui.TextDisabled(Unit.Local());
        base.TooltipAdditional();
    }
}
