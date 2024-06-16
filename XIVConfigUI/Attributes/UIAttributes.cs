namespace XIVConfigUI.Attributes;

/// <summary>
/// The attribute for the ui configs.
/// </summary>
/// <param name="name"></param>
/// <param name="filters">The filters</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public class UIAttribute(string name = "", params int[] filters) : Attribute
{
    /// <summary>
    /// The name of this config.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// The description about this ui item.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// The parent of this ui item.
    /// </summary>
    public string Parent { get; set; } = "";

    /// <summary>
    /// The filter to get this ui item.
    /// </summary>
    public int[] Filters => filters.Length == 0 ? [0] : filters;

    /// <summary>
    /// The order of this item.
    /// </summary>
    public byte Order { get; set; } = 0;

    /// <summary>
    /// The section of this item.
    /// </summary>
    public byte Section { get; set; } = 0;

    /// <summary>
    /// The action id 
    /// </summary>
    public uint Action { get; set; }
}
