using Dalamud.Interface.Internal;

namespace XIVConfigUI;

/// <summary>
/// The config of your serachable
/// </summary>
public abstract class SearchableConfig
{
    /// <summary>
    /// 
    /// </summary>
    public abstract bool ShowTooltip { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    public abstract bool IsPropertyValid(PropertyInfo property);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    public abstract void PropertyInvalidTooltip(PropertyInfo property);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    public abstract void PreNameDrawing(PropertyInfo property);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="texture"></param>
    /// <param name="loadingIcon"></param>
    /// <returns></returns>
    public abstract bool GetTexture(string path, out IDalamudTextureWrap texture, bool loadingIcon = false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="texture"></param>
    /// <param name="default"></param>
    /// <returns></returns>
    public abstract bool GetTexture(uint id, out IDalamudTextureWrap texture, uint @default = 0);
}
