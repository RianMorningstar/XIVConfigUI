namespace XIVConfigUI;

/// <summary>
/// The attribute for the ui configs.
/// </summary>
/// <param name="name"></param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class UIAttribute(string name) : Attribute
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
    public string Filter { get; set; } = "";

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
