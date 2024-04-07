using System.Reflection;

namespace XIVConfigUI.SearchableConfigs;

public class EnumSearch(PropertyInfo property, object obj) : Searchable(property, obj)
{
    protected int Value
    {
        get => Convert.ToInt32(_property.GetValue(_obj));
        set => _property.SetValue(_obj, Enum.ToObject(_property.PropertyType, value));
    }

    protected override void DrawMain()
    {
        var names = new List<string>();
        foreach (Enum v in Enum.GetValues(_property.PropertyType))
        {
            names.Add(v.Local());
        }
        var strs = names.ToArray();

        if (strs.Length > 0)
        {
            var value = Value;
            ImGui.SetNextItemWidth(Math.Max(ImGui.CalcTextSize(strs[value % strs.Length]).X + 30, DRAG_WIDTH) * Scale);
            if (ImGui.Combo($"##Config_{ID}{GetHashCode()}", ref value, strs, strs.Length))
            {
                Value = value;
            }
        }

        if (ImGui.IsItemHovered()) ShowTooltip();

        DrawName();
    }
}