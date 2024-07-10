using XIVConfigUI.Attributes;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The text item.
/// </summary>
/// <param name="property"></param>
/// <param name="obj"></param>
public class StringSearch(PropertyInfo property, object obj) : Searchable(property, obj)
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

        if (property.GetCustomAttribute<ChoicesAttribute>() is ChoicesAttribute choiceAttr)
        {
            var choices = choiceAttr.Choices;

            var values = choices.Select(i => i.Value).ToArray();
            var shows = choices.Select(i => i.Show).ToArray();
            var index = Array.IndexOf(values, value);

            if (ImGuiHelper.SelectableCombo($"##Config_{ID}{GetHashCode()}", shows, ref index))
            {
                Value = values[index];
            }
        }
        else
        {
            ImGui.SetNextItemWidth(DRAG_WIDTH * 1.5f * Scale);
            if (ImGui.InputText($"##Config_{ID}{GetHashCode()}", ref value, (uint)(Name.Length + 10)))
            {
                Value = value;
            }
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();

        DrawName();
    }
}
