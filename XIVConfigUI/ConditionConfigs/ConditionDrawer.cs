using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Newtonsoft.Json;
using System.Collections;
using System.Xml.Linq;
using XIVConfigUI.Attributes;

namespace XIVConfigUI.ConditionConfigs;

public static class ConditionDrawer
{
    private static float IconSizeRaw => ImGuiHelpers.GetButtonSize("H").Y;
    public static float IconSize => IconSizeRaw * ImGuiHelpers.GlobalScale;

    public static void Draw(object obj, JsonSerializerSettings? setting = null)
    {
        var type = obj.GetType();

        if (type.IsGenericType)
        {
            if(type.GetGenericTypeDefinition() == typeof(List<>) && obj is IList list)
            {
                var innerType = type.GetGenericArguments()[0];

                if (innerType.GetCustomAttributes().FirstOrDefault(i => i is ListUIAttribute) is ListUIAttribute attr)
                {
                    DrawList(list, attr, setting, innerType);
                }
            }
        }
        else
        {
            bool addSameLine = false;
            foreach (var prop in type.GetRuntimeProperties())
            {
                if (prop.GetCustomAttribute<UIAttribute>() is not UIAttribute uiAttribute) continue;
                if (addSameLine) ImGui.SameLine();
                addSameLine = true;

                DrawProperty(obj, prop, uiAttribute);
            }
        }
    }

    private static void DrawList(IList list, ListUIAttribute attr, JsonSerializerSettings? setting, Type innerType)
    {
        AddButton();

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            if (item == null) continue;

            void Delete()
            {
                list.RemoveAt(i);
            }

            void Up()
            {
                list.RemoveAt(i);
                list.Insert(Math.Max(0, i - 1), item);
            }

            void Down()
            {
                list.RemoveAt(i);
                list.Insert(Math.Min(list.Count, i + 1), item);
            }

            void Copy()
            {
                var str = JsonConvert.SerializeObject(list[i], Formatting.Indented, setting);
                ImGui.SetClipboardText(str);
            }

            var key = $"Item Edit Pop Up: {item.GetHashCode()}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
                (LocalString.Remove.Local(), Delete, ["Delete"]),
                (LocalString.MoveUp.Local(), Up, ["↑"]),
                (LocalString.MoveDown.Local(), Down, ["↓"]),
                (LocalString.CopyToClipboard.Local(), Copy, ["Ctrl"]));

            if (ImageLoader.GetTexture(attr.Icon, out var texture))
            {
                if (ImGuiHelper.SilenceImageButton(texture.ImGuiHandle, Vector2.One * IconSize, false, $"Icon :{item.GetHashCode()}"))
                {
                    attr.OnClick();
                }
            }

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, true,
                (Delete, [VirtualKey.DELETE]),
                (Up, [VirtualKey.UP]),
                (Down, [VirtualKey.DOWN]),
                (Copy, [VirtualKey.CONTROL]));

            ImGui.SameLine();

            using var grp = ImRaii.Group();

            Draw(item, setting);
        }

        void AddButton()
        {
            var hash = list.GetHashCode();
            if (!_creatableItems.TryGetValue(innerType, out var types))
            {
                types = innerType.Assembly.GetTypes().Where(t =>
                {
                    if (t.IsAbstract) return false;
                    if (t.GetConstructor([]) == null) return false;
                    return t.IsAssignableFrom(innerType);
                }).ToArray();
            }

            using (var iconFont = ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##AddButton{hash}"))
                {
                    if(types.Length == 1)
                    {
                        list.Add(Activator.CreateInstance(types[0]));
                    }
                    else
                    {
                        ImGui.OpenPopup("PopupButton" + hash);
                    }
                }
            }

            if (!string.IsNullOrEmpty(attr.Description))
            {
                ImGui.SameLine();
                ImGui.Text(innerType.Local("Description", attr.Description));
            }

            using var popUp = ImRaii.Popup("PopupButton" + hash);
            if (popUp)
            {
                foreach (var type in types)
                {
                    if (ImGui.Selectable(type.Local()))
                    {
                        list.Add(Activator.CreateInstance(type));
                        ImGui.CloseCurrentPopup();
                    }
                }

                if (ImGui.Selectable(LocalString.FromClipboard.Local()))
                {
                    var str = ImGui.GetClipboardText();
                    try
                    {
                        var s = JsonConvert.DeserializeObject(str, innerType, setting)!;
                        list.Add(s);
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Warning(ex, "Failed to load the condition.");
                    }
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private static readonly Dictionary<Type, Type[]> _creatableItems = [];

    private static void DrawProperty(object obj, PropertyInfo property, UIAttribute ui)
    {
        var propertyType = property.PropertyType;
        if (propertyType.IsEnum)
        {
            DrawEnum(obj, property);
        }
        else if(propertyType == typeof(string))
        {
            DrawString(obj, property);
        }
        else if(propertyType == typeof(bool))
        {
            DrawBool(obj, property);
        }
    }

    private static void DrawBool(object obj, PropertyInfo property)
    {
        var b = property.GetValue(obj) as bool?;
        if (b == null) return;
        var bo = b.Value;

        if (ImGui.Checkbox("##" + property.Name + obj.GetHashCode(), ref bo))
        {
            property.SetValue(obj, bo);
        }
        ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());
    }

    private static void DrawString(object obj, PropertyInfo property)
    {
        var str = property.GetValue(obj) as string;
        ImGui.SetNextItemWidth(Math.Max(80 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(str).X + 30 * ImGuiHelpers.GlobalScale));

        if (ImGui.InputTextWithHint($"##{property.Name}{obj.GetHashCode()}", property.LocalUIName(), ref str, 128))
        {
            property.SetValue(obj, str);
        }
        ImGuiHelper.HoveredTooltip(property.LocalUIDescription());
    }

    private static void DrawEnum(object obj, PropertyInfo property)
    {
        var value = property.GetValue(obj);
        var values = Enum.GetValues(property.PropertyType).Cast<Enum>().Where(i => i.GetAttribute<ObsoleteAttribute>() == null).ToHashSet().ToArray();
        var index = Array.IndexOf(values, value);
        var names = values.Select(v => v.Local()).ToArray();

        if (ImGuiHelper.SelectableCombo(property.Name + obj.GetHashCode(), names, ref index, description: property.LocalUINameDesc()))
        {
            property.SetValue(obj, values[index]);
        }
    }
}
