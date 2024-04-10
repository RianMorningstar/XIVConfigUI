using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;

namespace XIVConfigUI.SearchableConfigs;

/// <summary>
/// The checkbox item.
/// </summary>
public class CheckBoxSearch : Searchable
{
    /// <summary>
    /// The children of it.
    /// </summary>
    public List<Searchable> Children { get; } = [];

    /// <summary>
    /// The action id to show.
    /// </summary>
    public uint ActionId { get; init; } = 0;

    /// <summary>
    /// The additional draw of it.
    /// </summary>
    public Action? AdditionalDraw { get; set; } = null;

    /// <summary>
    /// Should it show the child.
    /// </summary>
    public virtual bool AlwaysShowChildren => false;

    /// <inheritdoc/>
    public override string Description => ActionId == 0 ? base.Description : ActionId.ToString();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="property"></param>
    /// <param name="obj"></param>
    /// <param name="children"></param>
    public CheckBoxSearch(PropertyInfo property, object obj,  params Searchable[] children)
        : base(property, obj)
    {
        ActionId = property.GetCustomAttribute<UIAttribute>()?.Action ?? 0;
        foreach (var child in children)
        {
            AddChild(child);
        }
    }

    /// <summary>
    /// Add child
    /// </summary>
    /// <param name="child"></param>
    public void AddChild(Searchable child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    /// <summary>
    /// The value.
    /// </summary>
    protected virtual bool Value
    {
        get => (bool)_property.GetValue(_obj)!;
        set
        {
            _property.SetValue(_obj, value);
            _config?.AfterConfigChange(this);
        }
    }

    /// <summary>
    /// To draw the children.
    /// </summary>
    protected virtual void DrawChildren()
    {
        var lastIs = false;
        foreach (var child in Children)
        {
            if (!child.ShowInChild) continue;

            var thisIs = child is CheckBoxSearch c && c.ActionId != 0 
                && ImageLoader.GetTextureAction(c.ActionId, out var texture);
            if (lastIs && thisIs)
            {
                ImGui.SameLine();
            }
            lastIs = thisIs;

            child.Draw();
        }
    }

    /// <summary>
    /// Draw the things in middle.
    /// </summary>
    protected virtual void DrawMiddle()
    {

    }

    /// <inheritdoc/>
    protected sealed override void DrawMain()
    {
        var hasChild = Children != null && Children.Any(c => c.ShowInChild);
        var hasAdditional = AdditionalDraw != null;
        var hasSub = hasChild || hasAdditional;
        IDalamudTextureWrap? texture = null;
        var hasIcon = ActionId != 0 && ImageLoader.GetTextureAction(ActionId, out texture);

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
            _config?.PreNameDrawing(_property);
            ImGui.SameLine();
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
