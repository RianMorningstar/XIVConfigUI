using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The drag int thing.
/// </summary>
public class DragIntSearch : Searchable
{
    /// <summary/>
    public RangeAttribute Range { get; }

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
        Range = _property.GetCustomAttribute<RangeAttribute>() ?? new();
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void DrawMain()
    {
        var value = Value;
        if (ImGuiHelper.DragInt($"##Config_{ID}{GetHashCode()}", DRAG_WIDTH, ref value, Range))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        DrawName();
    }
}
