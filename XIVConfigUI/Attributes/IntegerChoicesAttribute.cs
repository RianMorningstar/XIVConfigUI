namespace XIVConfigUI.Attributes;

/// <summary>
/// Get the Integer Choices.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class IntegerChoicesAttribute : Attribute
{
    /// <summary>
    /// Get Enum
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public abstract Type? GetEnumType(object obj);
}
