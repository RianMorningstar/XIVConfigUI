namespace XIVConfigUI.Attributes;

/// <summary>
/// The list item attribute.
/// </summary>
/// <param name="icon">the icon id.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ListUIAttribute(uint icon) : Attribute
{
    /// <summary>
    /// Description about this item.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Should this drawing has the new line.
    /// </summary>
    public bool NewlineWhenInheritance { get; set; }

    internal ListUIAttribute() : this(114053)
    {

    }

    /// <summary>
    /// When it clicked.
    /// </summary>
    /// <param name="obj">the instance of the class.</param>
    public virtual void OnClick(object obj) { }

    /// <summary>
    /// Get the icon
    /// </summary>
    /// <param name="obj">the instance of the class.</param>
    /// <returns></returns>
    public virtual uint GetIcon(object obj) { return icon; }
}
