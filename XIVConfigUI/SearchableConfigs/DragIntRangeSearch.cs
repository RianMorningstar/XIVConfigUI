namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The drag int.
/// </summary>
public class DragIntRangeSearch : Searchable
{
    /// <summary>
    /// 
    /// </summary>
    public int Min { get; }

    /// <summary>
    /// 
    /// </summary>
    public int Max { get; }

    /// <summary>
    /// 
    /// </summary>
    public float Speed { get; }

    /// <summary>
    /// 
    /// </summary>
    public ConfigUnitType Unit { get; }

    /// <summary>
    /// 
    /// </summary>
    protected Vector2Int Value
    {
        get => (Vector2Int)_property.GetValue(_obj)!;
        set => _property.SetValue(_obj, value);
    }

    /// <summary>
    /// 
    /// </summary>
    protected int MinValue
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
    protected int MaxValue
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
    /// 
    /// </summary>
    /// <param name="property"></param>
    /// <param name="obj"></param>
    public DragIntRangeSearch(PropertyInfo property, object obj) : base(property, obj)
    {
        var range = _property.GetCustomAttribute<RangeAttribute>();
        Min = (int?)range?.MinValue ?? 0;
        Max = (int?)range?.MaxValue ?? 1;
        Speed = range?.Speed ?? 0.001f;
        Unit = range?.UnitType ?? ConfigUnitType.None;
    }

    /// <inheritdoc/>
    protected override void DrawMain()
    {
        var minValue = MinValue;
        var maxValue = MaxValue;
        ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);
        if (ImGui.DragIntRange2($"##Config_{ID}{GetHashCode()}", ref minValue, ref maxValue, Speed, Min, Max))
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
