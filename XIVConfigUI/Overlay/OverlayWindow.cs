using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace XIVConfigUI.Overlay;
internal class OverlayWindow : Window
{
    public OverlayWindow() : base(XIVConfigUIMain.Command, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav, true)
    {
        IsOpen = true;
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);

        base.PreDraw();
    }

    public override void Draw()
    {
        ImGui.GetStyle().AntiAliasedFill = false;

        foreach (var element in XIVConfigUIMain._drawingElements)
        {
            element.Draw();
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        base.PostDraw();
    }
}
