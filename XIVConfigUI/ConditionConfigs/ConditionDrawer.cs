using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Newtonsoft.Json;
using System.Collections;
using System.Xml.Linq;
using XIVConfigUI.Attributes;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMJIFarmManagement;

namespace XIVConfigUI.ConditionConfigs;

public static class ConditionDrawer
{
    private static float IconSizeRaw => ImGuiHelpers.GetButtonSize("H").Y;
    public static float IconSize => IconSizeRaw * ImGuiHelpers.GlobalScale;

    public static void Draw(object obj)
    {
        var type = obj.GetType();

        if (IsList(type, out var innerType) && obj is IList list)
        {
            DrawList(list, innerType);
        }
        else
        {
            List<Action?> actions = [];
            bool addSameLine = false;
            foreach (var prop in type.GetRuntimeProperties())
            {
                if (prop.GetCustomAttribute<UIAttribute>() is not UIAttribute uiAttribute) continue;
                if (addSameLine) ImGui.SameLine();
                addSameLine = true;

                DrawProperty(obj, prop, uiAttribute, out var act);
                actions.Add(act);
            }

            foreach (var action in actions)
            {
                action?.Invoke();
            }
        }
    }

    private static bool IsList(Type type, out Type innerType)
    {
        if (type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            innerType = type.GetGenericArguments()[0];
            return true;
        }
        innerType = null!;
        return false;
    }

    private static void DrawList(IList list, Type innerType)
    {
        if (innerType.GetCustomAttributes().FirstOrDefault(i => i is ListUIAttribute) is not ListUIAttribute attr)
        {
            return;
        }

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
                var str = JsonConvert.SerializeObject(list[i], Formatting.Indented, GeneralJsonConverter.Instance);
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

            Draw(item);
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
                    return t.IsAssignableTo(innerType);
                }).ToArray();
            }

            using(var textColor = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow))
            {
                using var grp = ImRaii.Group();

                ImGui.Text($"{LocalString.List.Local()}: ");
                ImGui.SameLine();

                using var color = ImRaii.PushColor(ImGuiCol.Button, 0);

                var padding = ImGui.GetStyle().FramePadding;
                ImGui.GetStyle().FramePadding = Vector2.Zero;
                if (ImGui.Button($"{innerType.Local()} +##AddButton{hash}"))
                {
                    ImGui.OpenPopup("PopupButton" + hash);
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                ImGui.GetStyle().FramePadding = padding; ;
            }

            if (!string.IsNullOrEmpty(attr.Description))
            {
                ImGuiHelper.HoveredTooltip(innerType.Local("Description", attr.Description));
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
                        var s = JsonConvert.DeserializeObject(str, innerType, GeneralJsonConverter.Instance)!;
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

    private static void DrawProperty(object obj, PropertyInfo property, UIAttribute ui, out Action? drawSub)
    {
        drawSub = null;
        var propertyType = property.PropertyType;

        if (IsList(propertyType, out var innerType))
        {
            if (DrawSubItem(obj, property))
            {
                if (property.GetValue(obj) is IList list)
                {
                    drawSub = () => DrawList(list, innerType);
                }
            }
        }
        else if (propertyType.IsEnum)
        {
            DrawEnum(obj, property);
        }
        else if (propertyType == typeof(string))
        {
            DrawString(obj, property);
        }
        else if (propertyType == typeof(bool))
        {
            DrawBool(obj, property);
        }
        else if (propertyType == typeof(float))
        {
            DrawFloat(obj, property);
        }
        else if (propertyType.IsClass)
        {
            if (DrawSubItem(obj, property))
            {
                if (property.GetValue(obj) is object @class)
                {
                    drawSub = () => Draw(@class);
                }
            }
        }
    }

    private static readonly List<string> _showedItem = [];
    private static bool DrawSubItem(object obj, PropertyInfo property)
    {
        var key = property.Name + obj.GetHashCode();
        var opened = _showedItem.Contains(key);

        IDisposable? dispose = null;
        if (opened)
        {
            dispose = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
        }
        if (ImGui.Button($"{property.LocalUIName()}##{key}"))
        {
            if (opened)
            {
                _showedItem.Remove(key);
            }
            else
            {
                _showedItem.Add(key);
            }
        }
        dispose?.Dispose();
        ImGuiHelper.HoveredTooltip(property.LocalUIDescription());

        return opened;
    }

    private static void DrawFloat(object obj, PropertyInfo property)
    {
        var range = property.GetCustomAttribute<RangeAttribute>() ?? new();

        var value = (property.GetValue(obj) as float?)!.Value;

        if (ImGuiHelper.DragFloat("##" + property.Name + obj.GetHashCode(), 50, ref value, range))
        {
            property.SetValue(obj, value);
        }
        var tooltip = property.LocalUINameDesc();
        if (!string.IsNullOrEmpty(tooltip))
        {
            tooltip += "\n";
        }
        ImGuiHelper.HoveredTooltip(tooltip + range.UnitType.Local());
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
