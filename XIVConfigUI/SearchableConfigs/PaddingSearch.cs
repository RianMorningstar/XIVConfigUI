namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The padding search.
/// </summary>
public class PaddingSearch: Searchable
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

        ImGui.Text(Name);
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(DRAG_WIDTH * 0.5f * Scale);
        if (ImGui.DragFloat($"L##Config_{ID}{GetHashCode()}", ref value.X, Speed, Min, Max, $"{value.X:F2}{Unit.ToSymbol()}"))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(DRAG_WIDTH * 0.5f * Scale);
        if (ImGui.DragFloat($"T##Config_{ID}{GetHashCode()}", ref value.Y, Speed, Min, Max, $"{value.Y:F2}{Unit.ToSymbol()}"))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(DRAG_WIDTH * 0.5f * Scale);
        if (ImGui.DragFloat($"R##Config_{ID}{GetHashCode()}", ref value.Z, Speed, Min, Max, $"{value.Z:F2}{Unit.ToSymbol()}"))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(DRAG_WIDTH * 0.5f * Scale);
        if (ImGui.DragFloat($"B##Config_{ID}{GetHashCode()}", ref value.W, Speed, Min, Max, $"{value.W:F2}{Unit.ToSymbol()}"))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();
    }

    /// <inheritdoc/>
    protected override void TooltipAdditional()
    {
        ImGui.Separator();
        ImGui.TextDisabled(Unit.Local());
        base.TooltipAdditional();
    }
}
