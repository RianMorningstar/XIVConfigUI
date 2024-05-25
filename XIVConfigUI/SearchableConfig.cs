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
    public virtual Dictionary<string, Func<PropertyInfo, Searchable>>  PropertyNameCreators { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    public virtual  Dictionary<Type, Func<PropertyInfo, Searchable>> PropertyTypeCreators { get; } = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    public virtual bool IsPropertyValid(PropertyInfo property) => true;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    public virtual void PropertyInvalidTooltip(PropertyInfo property) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="property"></param>
    public virtual void PreNameDrawing(PropertyInfo property) { }

    /// <summary>
    /// 
    /// </summary>
    public virtual void AfterConfigChange(Searchable item) { }
}
