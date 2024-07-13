using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections;
using XIVConfigUI.Attributes;

namespace XIVConfigUI.ConditionConfigs;

/// <summary>
/// The drawer for condition style.
/// </summary>
public static class ConditionDrawer
{
    /// <summary>
    /// The custom way to draw a condition.
    /// </summary>
    public static Dictionary<Type, Func<object, PropertyInfo, Action?>> CustomDrawings { get; } = [];

    /// <summary>
    /// The custom list item ui list.
    /// </summary>
    public static Dictionary<Type, ListUIAttribute> CustomListUIs { get; } = [];

    private static float IconSizeRaw => ImGuiHelpers.GetButtonSize("H").Y;

    /// <summary>
    /// The size for condition icon.
    /// </summary>
    public static float IconSize => IconSizeRaw * ImGuiHelpers.GlobalScale;

    /// <summary>
    /// The main method to draw a condition.
    /// </summary>
    /// <param name="obj"></param>
    public static void Draw(object? obj)
    {
        if (obj == null) return;

        var type = obj.GetType();

        if (IsList(type, out var innerType) && obj is IList list)
        {
            DrawList(list, innerType);
        }
        else 
        {
            List<Action?> actions = [];
            bool addSameLine = false;

            var allProps = type.GetRuntimeProperties();
            var propsGrp = allProps.GroupBy(p => p.DeclaringType!).ToDictionary(i => i.Key, i => i.ToList());
            var methodsGrp = type.GetRuntimeMethods().GroupBy(p => p.DeclaringType!).ToDictionary(i => i.Key, i => i.ToList());

            var keys = propsGrp.Keys.Union(methodsGrp.Keys).OrderBy(CountOfInheritance);

            var attr = obj.GetType().GetCustomAttribute<ListUIAttribute>(false) 
                ?? obj.GetType().GetCustomAttribute<ListUIAttribute>();
            var newline = attr?.NewlineWhenInheritance ?? false;

            foreach (var key in keys)
            {
                if (!propsGrp.TryGetValue(key, out var props)) props = [];
                if (!methodsGrp.TryGetValue(key, out var methods)) methods = [];

                if (newline) addSameLine = false;

                foreach (var prop in props)
                {
                    if (prop.GetCustomAttribute<UIAttribute>() is not UIAttribute uiAttribute) continue;
                    if(!IsAttributeValid(uiAttribute)) continue;

                    if (addSameLine) ImGui.SameLine();
                    addSameLine = true;

                    DrawProperty(obj, prop, out var act);
                    actions.Add(act);
                }

                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute<UIAttribute>() is not UIAttribute uiAttribute) continue;
                    if (method.ReturnType != typeof(void)) continue;
                    if (method.GetParameters().Length != 0) continue;
                    if (!IsAttributeValid(uiAttribute)) continue;

                    if (addSameLine) ImGui.SameLine();
                    addSameLine = true;

                    if (ImGui.Button(method.LocalUIName() + "##" + method.Name + obj.GetHashCode()))
                    {
                        method.Invoke(obj, []);
                    }
                    ImGuiHelper.HoveredTooltip(method.LocalUIDesc());
                }
            }

            if (!actions.Any(i => i is not null)) return;

            ImGui.Text("    ");
            ImGui.SameLine();

            using var grp = ImRaii.Group();
            foreach (var action in actions)
            {
                action?.Invoke();
            }

            bool IsAttributeValid(UIAttribute uiAttribute)
            {
                var parent = uiAttribute.Parent;
                if (!string.IsNullOrEmpty(parent))
                {
                    var parentProp = allProps.FirstOrDefault(p => p.Name == parent);
                    if (parentProp != null)
                    {
                        var v = parentProp.GetValue(obj);
                        if (v is bool b)
                        {
                            if (!b) return false;
                        }
                        else
                        {
                            try
                            {
                                if (!uiAttribute.Filters.Contains(Convert.ToInt32(v))) return false;
                            }
                            catch (Exception ex)
                            {
                                Service.Log.Error(ex, "Failed to check the filter");
                            }
                        }

                        if (parentProp.GetCustomAttribute<UIAttribute>() is UIAttribute ui)
                        {
                            if (!IsAttributeValid(ui)) return false;
                        }
                    }
                }
                return true;
            }
        }
    }

    private static int CountOfInheritance(Type? type)
    {
        int result = 0;
        while(type != null)
        {
            result++;
            type = type.BaseType;
        }
        return result;
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
        var attrBase = innerType.GetCustomAttribute<ListUIAttribute>();

        if (attrBase == null)
        {
            CustomListUIs.TryGetValue(innerType, out attrBase);
        }

        attrBase ??= new();

        AddButton(-1);

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            if (item == null) continue;
            var t = item.GetType();
            var attr = t.GetCustomAttribute<ListUIAttribute>(false) ?? attrBase;

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
                var str = JsonHelper.SerializeObject(item);
                ImGui.SetClipboardText(str);
            }

            void Add()
            {
                AddButton(i);
            }

            var key = $"Item Edit Pop Up: {item.GetHashCode()}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
                (LocalString.Remove.Local(), Delete, ["Delete"]),
                (LocalString.MoveUp.Local(), Up, ["↑"]),
                (LocalString.MoveDown.Local(), Down, ["↓"]),
                (LocalString.Add.Local(), Add, ["+"]),
                (LocalString.CopyToClipboard.Local(), Copy, ["Ctrl"]));

            if (item is ICondition condition)
            {
                DrawCondition(condition.State, $"Icon :{item.GetHashCode()}", () => attr.OnClick(item));
            }
            else if(attr.GetIcon(item) is uint icon && icon != 0)
            {
                if (ImageLoader.GetTexture(icon, out var texture))
                {
                    if (ImGuiHelper.SilenceImageButton(texture.ImGuiHandle, Vector2.One * IconSize, false, $"Icon :{item.GetHashCode()}"))
                    {
                        attr.OnClick(item);
                    }
                }
            }
            else
            {
                DrawCondition(null, $"Icon :{item.GetHashCode()}", () => attr.OnClick(item));
            }

            string desc = item.GetType().Local();
            var description = attr.GetDescription(item);
            if (!string.IsNullOrEmpty(description))
            {
                desc += "\n" + innerType.Local("Description", description);
            }

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, desc, true,
                (Delete, [VirtualKey.DELETE]),
                (Up, [VirtualKey.UP]),
                (Down, [VirtualKey.DOWN]),
                (Add, [VirtualKey.ADD]),
                (Copy, [VirtualKey.CONTROL]));

            ImGui.SameLine();

            using var grp = ImRaii.Group();

            Draw(item);
        }

        void AddButton(int index)
        {
            var hash = list.GetHashCode();
            if (!_creatableItems.TryGetValue(innerType, out var types))
            {
                _creatableItems[innerType] = types = innerType.Assembly.GetTypes().Append(innerType).Where(t =>
                {
                    if (t.IsAbstract) return false;
                    if (t.IsClass && t.GetConstructor([]) == null) return false;
                    return t.IsAssignableTo(innerType);
                }).ToHashSet().ToArray();
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

            if (!string.IsNullOrEmpty(attrBase.Description))
            {
                ImGuiHelper.HoveredTooltip(innerType.Local("Description", attrBase.Description));
            }

            using var popUp = ImRaii.Popup("PopupButton" + hash);
            if (popUp)
            {
                foreach (var type in types)
                {
                    if (ImGui.Selectable(type.Local()))
                    {
                        var instance = Activator.CreateInstance(type);
                        if (index == -1)
                        {
                            list.Add(instance);
                        }
                        else
                        {
                            list.Insert(index, instance);
                        }
                        
                        ImGui.CloseCurrentPopup();
                    }
                }

                if (ImGui.Selectable(LocalString.FromClipboard.Local()))
                {
                    var str = ImGui.GetClipboardText();
                    try
                    {
                        var instance = JsonHelper.DeserializeObject(str, innerType)!;
                        if (index == -1)
                        {
                            list.Add(instance);
                        }
                        else
                        {
                            list.Insert(index, instance);
                        }
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

    private static void DrawProperty(object obj, PropertyInfo property, out Action? drawSub)
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
        else if(CustomDrawings.TryGetValue(propertyType, out var method))
        {
            drawSub = method(obj, property);
        }
        else if (propertyType.IsEnum)
        {
            DrawEnum(obj, property);
        }
        else if (propertyType == typeof(int))
        {
            DrawInt(obj, property);
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
        else if(propertyType == typeof(Vector2))
        {
            DrawFloat2(obj, property);
        }
        else if (propertyType == typeof(Vector2Int))
        {
            DrawInt2(obj, property);
        }
        else if (propertyType == typeof(Vector4))
        {
            DrawFloat4(obj, property);
        }
        else if (propertyType.IsClass || propertyType.IsInterface)
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

    private static readonly Dictionary<int, string> _showedItem = [];
    private static bool DrawSubItem(object obj, PropertyInfo property)
    {
        var id = obj.GetHashCode();
        var name = property.Name;
        var opened = _showedItem.TryGetValue(id, out var savedName) && savedName == name;

        IDisposable? dispose = null;
        if (opened)
        {
            dispose = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
        }
        if (ImGui.Button($"{property.LocalUIName()}##{name}{id}"))
        {
            if (opened)
            {
                _showedItem.Remove(id);
            }
            else
            {
                _showedItem[id] = name;
            }
        }
        dispose?.Dispose();
        ImGuiHelper.HoveredTooltip(property.LocalUIDesc());

        return opened;
    }
    private static void DrawFloat4(object obj, PropertyInfo property)
    {
        var value = (property.GetValue(obj) as Vector4?)!.Value;
        var type = property.GetCustomAttribute<UITypeAttribute>()?.Type ?? UiType.Color;

        switch(type)
        {
            case UiType.Padding:
                var range = property.GetCustomAttribute<RangeAttribute>() ?? new();

                if (ImGuiHelper.DragFloat4("##" + property.Name + obj.GetHashCode(), 50, ref value, range))
                {
                    property.SetValue(obj, value);
                }
                ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());
                break;

            default:
                if (ImGui.ColorEdit4("##" + property.Name + obj.GetHashCode(), ref value, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoTooltip))
                {
                    property.SetValue(obj, value);
                }
                ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());

                break;
        }
    }

    private static void DrawInt2(object obj, PropertyInfo property)
    {
        var range = property.GetCustomAttribute<RangeAttribute>() ?? new();

        var value = (property.GetValue(obj) as Vector2Int?)!.Value;

        if (ImGuiHelper.DragInt2("##" + property.Name + obj.GetHashCode(), 50, ref value, range))
        {
            property.SetValue(obj, value);
        }
        ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());
    }

    private static void DrawFloat2(object obj, PropertyInfo property)
    {
        var range = property.GetCustomAttribute<RangeAttribute>() ?? new();

        var value = (property.GetValue(obj) as Vector2?)!.Value;

        var type = property.GetCustomAttribute<UITypeAttribute>()?.Type ?? UiType.Range;

        switch (type)
        {
            case UiType.Padding:
                using (var group = ImRaii.Group())
                {
                    var v = value.X;
                    if (ImGuiHelper.DragFloat("X##" + property.Name + obj.GetHashCode(), 50, ref v, range))
                    {
                        property.SetValue(obj, new Vector2(v, value.Y));
                    }
                    ImGui.SameLine();
                    v = value.Y;
                    if (ImGuiHelper.DragFloat("Y##" + property.Name + obj.GetHashCode(), 50, ref v, range))
                    {
                        property.SetValue(obj, new Vector2(value.X, v));
                    }
                }

                break;

            default:
                if (ImGuiHelper.DragFloat2("##" + property.Name + obj.GetHashCode(), 50, ref value, range))
                {
                    property.SetValue(obj, value);
                }
                break;
        }
        ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());
    }

    private static void DrawInt(object obj, PropertyInfo property)
    {
        var range = property.GetCustomAttribute<RangeAttribute>() ?? new();
        var value = (property.GetValue(obj) as int?)!.Value;

        if (property.GetCustomAttribute<IntegerChoicesAttribute>() is IntegerChoicesAttribute attr
            && attr.GetEnumType(obj) is Type type && type.IsEnum)
        {
            var @enum = (Enum) Enum.ToObject(type, value);
            if (ImGuiHelper.SelectableCombo(property.Name + obj.GetHashCode(), ref @enum, description: property.LocalUINameDesc()))
            {
                property.SetValue(obj, Convert.ToInt32(@enum));
            }
        }
        else
        {
            if (ImGuiHelper.DragInt("##" + property.Name + obj.GetHashCode(), 50, ref value, range))
            {
                property.SetValue(obj, value);
            }

            ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());
        }
    }

    private static void DrawFloat(object obj, PropertyInfo property)
    {
        var range = property.GetCustomAttribute<RangeAttribute>() ?? new();

        var value = (property.GetValue(obj) as float?)!.Value;

        if (ImGuiHelper.DragFloat("##" + property.Name + obj.GetHashCode(), 50, ref value, range))
        {
            property.SetValue(obj, value);
        }
        ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());
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
        var str = (property.GetValue(obj) as string)!;

        if (property.SetMethod?.IsPrivate ?? true)
        {
            ImGui.Text(str);
            return;
        }

        if (property.GetCustomAttribute<ChoicesAttribute>() is ChoicesAttribute choiceAttr)
        {
            var choices = choiceAttr.Choices;

            var values = choices.Select(i => i.Value).ToArray();
            var shows = choices.Select(i => i.Show).ToArray();
            var index = Array.IndexOf(values, str);

            if (ImGuiHelper.SelectableCombo($"##{property.Name}{obj.GetHashCode()}Selector", shows,
                ref index, description: property.LocalUINameDesc()))
            {
                property.SetValue(obj, values[index]);
            }
            return;
        }

        var uiType = property.GetCustomAttribute<UITypeAttribute>()?.Type ?? UiType.OneLine;

        switch (uiType)
        {
            case UiType.Multiline:
                if (ImGui.InputTextMultiline($"##{property.Name}{obj.GetHashCode()}", ref str, 1024,
                    new Vector2(-1, 50 * ImGuiHelpers.GlobalScale)))
                {
                    property.SetValue(obj, str);
                }
                ImGuiHelper.HoveredTooltip(property.LocalUINameDesc());

                break;

            default:
                ImGuiHelper.SetNextWidthWithName(str);

                if (ImGui.InputTextWithHint($"##{property.Name}{obj.GetHashCode()}", property.LocalUIName(), ref str, 128))
                {
                    property.SetValue(obj, str);
                }
                ImGuiHelper.HoveredTooltip(property.LocalUIDesc());

                break;
        }
    }

    private static void DrawEnum(object obj, PropertyInfo property)
    {
        var value = (Enum)property.GetValue(obj)!;
        
        if (ImGuiHelper.SelectableCombo(property.Name + obj.GetHashCode(), ref value, description: property.LocalUINameDesc()))
        {
            property.SetValue(obj, value);
        }
    }

    internal static void DrawCondition(bool? tag, string id, Action? action = null, uint buttonColor = 0)
    {
        float size = IconSize * (1 + (8 / 82));
        if (!tag.HasValue)
        {
            if (ImageLoader.GetTexture("ui/uld/image2.tex", out var texture) || ImageLoader.GetTexture(0u, out texture))
            {
                if (ImGuiHelper.SilenceImageButton(texture.ImGuiHandle, Vector2.One * size, false, id))
                {
                    action?.Invoke();
                }
            }
        }
        else
        {
            if (ImageLoader.GetTexture("ui/uld/readycheck_hr1.tex", out var texture))
            {
                if (ImGuiHelper.SilenceImageButton(texture.ImGuiHandle, Vector2.One * size,
                    new Vector2(tag.Value ? 0 : 0.5f, 0),
                    new Vector2(tag.Value ? 0.5f : 1, 1), buttonColor, id))
                {
                    action?.Invoke();
                }
            }
        }
    }
}
