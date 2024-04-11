using Dalamud.Interface.Internal;

namespace XIVConfigUI;

/// <summary>
/// The config items.
/// </summary>
public abstract class ConfigWindowItem
{
    /// <summary>
    /// Skip for drawing in the list.
    /// </summary>
    public virtual bool IsSkip => false;

    /// <summary>
    /// The name of it.
    /// </summary>
    public virtual string Name => GetType().Local();

    /// <summary>
    /// The description.
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    /// The way to get the icon.
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public abstract bool GetIcon(out IDalamudTextureWrap texture);

    /// <summary>
    /// Draw on the body.
    /// </summary>
    /// <param name="window"></param>
    public abstract void Draw(ConfigWindow window);
}
