using Dalamud.Interface.Internal;

namespace XIVConfigUI;

public abstract class ConfigWindowItem
{
    public virtual bool IsSkip => false;
    public string Name => GetType().Local();
    public virtual string Description => string.Empty;
    public abstract bool GetIcon(out IDalamudTextureWrap texture);
    public abstract void Draw(ConfigWindow window);
}
