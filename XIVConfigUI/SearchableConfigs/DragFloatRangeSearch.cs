using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The drag float thing.
/// </summary>
public class DragFloatRangeSearch : Searchable
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
    /// Type.
    /// </summary>
    public ConfigUnitType Unit { get; }

    /// <summary>
    /// Value.
    /// </summary>
    protected Vector2 Value
    {
        get => (Vector2)_property.GetValue(_obj)!;
        set
        {
            _property.SetValue(_obj, value);
            _config?.AfterConfigChange(this);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected float MinValue 
    {
        get => Value.X;
        set
        {
            var v = Value;
            v.X = value;
            Value = v;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    protected float MaxValue
    {
        get => Value.Y;
        set
        {
            var v = Value;
            v.Y = value;
            Value = v;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="property"></param>
    /// <param name="obj"></param>
    public DragFloatRangeSearch(PropertyInfo property, object obj) : base(property, obj)
    {
        var range = _property.GetCustomAttribute<RangeAttribute>() ?? new();
        Min = range.MinValue;
        Max = range.MaxValue;
        Speed = range.Speed;
        Unit = range.UnitType;
    }

    /// <inheritdoc/>
    protected override void DrawMain()
    {
        var minValue = MinValue;
        var maxValue = MaxValue;
        ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);

        if (ImGui.DragFloatRange2($"##Config_{ID}{GetHashCode()}", ref minValue, ref maxValue, Speed, Min, Max,
     Unit == ConfigUnitType.Percent ? $"{minValue * 100:F1}{Unit.ToSymbol()}" : $"{minValue:F2}{Unit.ToSymbol()}",
    Unit == ConfigUnitType.Percent ? $"{maxValue * 100:F1}{Unit.ToSymbol()}" : $"{maxValue:F2}{Unit.ToSymbol()}"))
        {
            MinValue = Math.Min(minValue, maxValue);
            MaxValue = Math.Max(minValue, maxValue);
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
