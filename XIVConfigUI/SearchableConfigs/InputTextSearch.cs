namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The text item.
/// </summary>
/// <param name="property"></param>
/// <param name="obj"></param>
public class InputTextSearch(PropertyInfo property, object obj) : Searchable(property, obj)
{
    /// <summary>
    /// Value
    /// </summary>
    protected string Value
    {
        get => (string)_property.GetValue(_obj)!;
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
        if (ImGui.InputText($"##Config_{ID}{GetHashCode()}", ref value, (uint)(Name.Length + 10)))
        {
            Value = value;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();

        DrawName();
    }
}
