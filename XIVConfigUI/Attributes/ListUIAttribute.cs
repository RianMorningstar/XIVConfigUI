namespace XIVConfigUI.Attributes;

/// <summary>
/// The list item attribute.
/// </summary>
/// <param name="icon">the icon id.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ListUIAttribute(uint icon) : Attribute
{
    /// <summary>
    /// The icon.
    /// </summary>
    public uint Icon => icon;

    /// <summary>
    /// Description about this item.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Should this drawing has the new line.
    /// </summary>
    public bool NewlineWhenInheritance { get; set; }

    internal ListUIAttribute() : this(0)
    {

    }

    /// <summary>
    /// When it clicked.
    /// </summary>
    /// <param name="obj">the instance of the class.</param>
    public virtual void OnClick(object obj) { }
}
