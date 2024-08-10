namespace XIVConfigUI.Overlay;
public abstract class BaseDraw : IDisposable
{
    public bool Enable { get; set; } = true;

    public Action? UpdateEveryFrame { get; set; }

    protected BaseDraw()
    {
        XIVConfigUIMain._drawingElements.Add(this);
        Service.Framework.Update += Framework_Update;
    }

    public void Dispose()
    {
        XIVConfigUIMain._drawingElements.Remove(this);
        Service.Framework.Update -= Framework_Update;
    }

    private void Framework_Update(Dalamud.Plugin.Services.IFramework framework)
    {
        UpdateEveryFrame?.Invoke();
    }

    public void Draw()
    {
        if (!Enable) return;
        DrawInside();
    }

    protected abstract void DrawInside();
}
