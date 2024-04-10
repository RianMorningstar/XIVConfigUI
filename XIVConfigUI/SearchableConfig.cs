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
}
