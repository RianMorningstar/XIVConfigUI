namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The color item.
/// </summary>
/// <param name="property"></param>
/// <param name="obj"></param>
public class ColorEditSearch(PropertyInfo property, object obj) : Searchable(property, obj)
{
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

    /// <inheritdoc/>
    protected override void DrawMain()
    {
        var value = Value;
        ImGui.SetNextItemWidth(DRAG_WIDTH * 1.5f * Scale);
        if (ImGui.ColorEdit4($"{Name}##Config_{ID}{GetHashCode()}", ref value))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();
    }
}
