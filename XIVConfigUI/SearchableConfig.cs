using XIVConfigUI.SearchableConfigs;

namespace XIVConfigUI;

/// <summary>
/// The config of your serachable
/// </summary>
public abstract class SearchableConfig
{
    /// <summary>
    /// 
    /// </summary>
    public virtual bool GeneratDefault { get; } = true;

    /// <summary>
    /// 
    /// </summary>
    public virtual Dictionary<string, Func<PropertyInfo, Searchable>>  PropertyNameCreaters { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    public virtual  Dictionary<Type, Func<PropertyInfo, Searchable>> PropertyTypeCreaters { get; } = [];

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
    public virtual void AfterConfigChange(Searchable item)
    {

    }
}
