using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using System.Numerics;
using System.Reflection;

namespace XIVConfigUI.SearchableConfigs;
internal class CheckBoxSearch : Searchable
{
    public List<Searchable> Children { get; } = [];

    public uint Action { get; init; } = 0;

    public Action? AdditionalDraw { get; set; } = null;

    public virtual bool AlwaysShowChildren => false;

    public override string Description => Action == 0 ? base.Description : Action.ToString();

    internal CheckBoxSearch(PropertyInfo property, object obj,  params Searchable[] children)
        : base(property, obj)
    {
        Action = property.GetCustomAttribute<UIAttribute>()?.Action ?? 0;
        foreach (var child in children)
        {
            AddChild(child);
        }
    }

    public void AddChild(Searchable child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    protected bool Value
    {
        get => (bool)_property.GetValue(_obj)!;
        set => _property.SetValue(_obj, value);
    }

    protected virtual void DrawChildren()
    {
        var lastIs = false;
        foreach (var child in Children)
        {
            if (!child.ShowInChild) continue;

            var thisIs = child is CheckBoxSearch c && c.Action != 0 
                && XIVConfigUIMain.GetTextureAction(c.Action, out var texture);
            if (lastIs && thisIs)
            {
                ImGui.SameLine();
            }
            lastIs = thisIs;

            child.Draw();
        }
    }

    protected virtual void DrawMiddle()
    {

    }

    protected override void DrawMain()
    {
        var hasChild = Children != null && Children.Any(c => c.ShowInChild);
        var hasAdditional = AdditionalDraw != null;
        var hasSub = hasChild || hasAdditional;
        IDalamudTextureWrap? texture = null;
        var hasIcon = Action != 0 && XIVConfigUIMain.GetTextureAction(Action, out texture);

        var enable = Value;
        if (ImGui.Checkbox($"##{ID}", ref enable))
        {
            Value = enable;
        }
        if (ImGui.IsItemHovered()) ShowTooltip();

        ImGui.SameLine();

        var name = $"{Name}##Config_{ID}{GetHashCode()}";
        if (hasIcon)
        {
            ImGui.BeginGroup();
            var cursor = ImGui.GetCursorPos();
            var size = ImGuiHelpers.GlobalScale * 32;
            if (ImGuiHelper.NoPaddingNoColorImageButton(texture!.ImGuiHandle, Vector2.One * size, ID))
            {
                Value = enable;
            }
            ImGuiHelper.DrawActionOverlay(cursor, size, enable ? 1 : 0);
            ImGui.EndGroup();

            if (ImGui.IsItemHovered()) ShowTooltip();
        }
        else if (hasSub)
        {
            if (enable || AlwaysShowChildren)
            {
                var x = ImGui.GetCursorPosX();
                DrawMiddle();
                var drawBody = ImGui.TreeNode(name);
                if (ImGui.IsItemHovered()) ShowTooltip();

                if (drawBody)
                {
                    ImGui.SetCursorPosX(x);
                    ImGui.BeginGroup();
                    AdditionalDraw?.Invoke();
                    if (hasChild)
                    {
                        DrawChildren();
                    }
                    ImGui.EndGroup();
                    ImGui.TreePop();
                }
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0x0);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0x0);
                ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                if (ImGui.IsItemHovered()) ShowTooltip(false);

                ImGui.PopStyleColor(2);
            }
        }
        else
        {
            DrawName();
        }
    }
}
