using Dalamud.Interface.Utility.Raii;

namespace XIVConfigUI;

/// <summary>
/// 
/// </summary>
public enum HeaderSizeType : byte
{
    /// <summary>
    /// 
    /// </summary>
    First,

    /// <summary>
    /// 
    /// </summary>
    Second,

    /// <summary>
    /// 
    /// </summary>
    Third,

    /// <summary>
    /// 
    /// </summary>
    Forth,

    /// <summary>
    /// 
    /// </summary>
    Fifth,
}

/// <summary>
/// The collapsing header group for simplify the drawing.
/// </summary>
public class CollapsingHeaderGroup()
{
    private readonly Dictionary<Func<string>, Action> _headers = [];
    private int _openedIndex = -1;

    /// <summary>
    /// The size of the header.
    /// </summary>
    public HeaderSizeType HeaderSize { get; set; } = HeaderSizeType.Third;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="headers"></param>
    public CollapsingHeaderGroup(Dictionary<Func<string>, Action> headers) : this()
    {
        _headers = headers;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public void AddCollapsingHeader(Func<string> name, Action action)
    {
        _headers.Add(name, action);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    public void RemoveCollapsingHeader(Func<string> name)
    {
        _headers.Remove(name);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ClearCollapsingHeader()
    {
        _headers.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
    public void Draw()
    {
        var index = -1;
        foreach (var header in _headers)
        {
            index++;

            if (header.Key == null) continue;
            if (header.Value == null) continue;

            var name = header.Key();
            if (string.IsNullOrEmpty(name)) continue;

            try
            {
                ImGui.Spacing();
                ImGui.Separator();
                var selected = index == _openedIndex;
                var changed = false;
                using (var font = ImRaii.PushFont(ImGuiHelper.GetFont(HeaderSize switch 
                { 
                    HeaderSizeType.First => 72,
                    HeaderSizeType.Second => 48,
                    HeaderSizeType.Third => 32,
                    HeaderSizeType.Forth => 24,
                    _ => 18,
                })))
                {
                    changed = ImGui.Selectable(name, selected, ImGuiSelectableFlags.DontClosePopups);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                if (changed)
                {
                    _openedIndex = selected ? -1 : index;
                }
                if (selected)
                {
                    header.Value();
                }
            }
            catch (Exception ex)
            {
                Service.Log.Warning(ex, "Something wrong with header drawing.");
            }
        }
    }
}