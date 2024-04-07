using System.Numerics;
using System.Reflection;

namespace XIVConfigUI.SearchableConfigs;

internal class ColorEditSearch(PropertyInfo property, object obj) : Searchable(property, obj)
{
    protected Vector4 Value 
    {
        get => (Vector4)_property.GetValue(_obj)!;
        set => _property.SetValue(_obj, value);
    }

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
