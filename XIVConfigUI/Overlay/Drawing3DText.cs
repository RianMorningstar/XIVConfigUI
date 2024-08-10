using Dalamud.Interface.Utility.Raii;

namespace XIVConfigUI.Overlay;

/// <summary>
/// The text drawing.
/// </summary>
/// <param name="text"></param>
/// <param name="position"></param>
public class Drawing3DText(string text, Vector3 position) : BaseDraw
{
    /// <summary>
    /// The text it should show.
    /// </summary>
    public string Text { get; set; } = text;

    /// <summary>
    /// The location of showing.
    /// </summary>
    public Vector3 Position { get; set; } = position;

    /// <summary>
    /// Should it hides if the <seealso cref="Position"/> can't be seen by the active camera.
    /// </summary>
    public bool HideIfInvisible { get; set; }

    /// <summary>
    /// The padding of the bg.
    /// </summary>
    public Vector2 Padding { get; set; } = Vector2.One * 5;

    /// <summary>
    /// The size of the text.
    /// </summary>
    public float Scale { get; set; } = 1;

    /// <summary>
    /// 
    /// </summary>
    public uint Color { get; set; } = uint.MaxValue;

    /// <summary>
    /// The background Color.
    /// </summary>
    public uint BackgroundColor { get; set; } = 0x00000080;

    /// <summary>
    /// The corner of the background.
    /// </summary>
    public float Corner { get; set; } = 5;

    /// <summary>
    /// Convert this to the 2d elements.
    /// </summary>
    /// <returns></returns>
    protected override void DrawInside()
    {
        if (HideIfInvisible && !Position.CanSee() || string.IsNullOrEmpty(Text)) return;

        var pts = DrawingExtensions.GetPtsOnScreen([Position], false, false);
        if (pts.Length == 0) return;
        var pt = pts[0];

        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Padding);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, Corner);
        using var bgColor = ImRaii.PushColor(ImGuiCol.ChildBg, BackgroundColor);
        using var textColor = ImRaii.PushColor(ImGuiCol.Text, Color);
        using var font = ImRaii.PushFont(ImGuiHelper.GetFont(ImGui.GetFontSize() * Scale));

        var size = ImGui.CalcTextSize(Text);
        size += Padding * 2;
        ImGui.SetNextWindowPos(new Vector2(pt.X - size.X / 2, pt.Y - size.Y / 2));
        using var child = ImRaii.Child("##TextChild" + GetHashCode().ToString(), size, false,
            ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysUseWindowPadding);

        ImGui.Text(Text);
    }
}