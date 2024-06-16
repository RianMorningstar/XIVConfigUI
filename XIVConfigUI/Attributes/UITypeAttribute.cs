namespace XIVConfigUI.Attributes;

/// <summary>
/// The ui type attribute
/// </summary>
/// <param name="type"></param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class UITypeAttribute(UiType type) : Attribute
{
    /// <summary>
    /// tye type.
    /// </summary>
    public UiType Type => type;
}

/// <summary>
/// Some special type
/// </summary>
public enum UiType : byte
{
    /// <summary/>
    Color,

    /// <summary/>
    Padding,

    /// <summary/>
    OneLine,

    /// <summary/>
    Multiline,
}