using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The drag int thing.
/// </summary>
public class DragIntSearch : Searchable
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
    protected int Value
    {
        get => (int)_property.GetValue(_obj)!;
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
    public DragIntSearch(PropertyInfo property, object obj) : base(property, obj)
    {
        var range = _property.GetCustomAttribute<RangeAttribute>();
        Min = (int?)range?.MinValue ?? 0;
        Max = (int?)range?.MaxValue ?? 1;
        Speed = range?.Speed ?? 0.001f;
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void DrawMain()
    {
        var value = Value;

        ImGui.SetNextItemWidth(Scale * DRAG_WIDTH);
        if (ImGui.DragInt($"##Config_{ID}{GetHashCode()}", ref value, Speed, Min, Max))
        {
            Value = value;
        }

        if (ImGui.IsItemHovered()) ShowTooltip();

        DrawName();
    }
}
