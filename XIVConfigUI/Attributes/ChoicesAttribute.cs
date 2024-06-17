namespace XIVConfigUI.Attributes;

/// <summary>
/// The choices of the string.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class ChoicesAttribute : Attribute
{
    /// <summary>
    /// Use lazy to let it not copy it every time.
    /// </summary>
    protected virtual bool Lazy => true;
    private readonly Lazy<Pair[]> _choicesCreator;
    /// <summary>
    /// The choices.
    /// </summary>
    public Pair[] Choices => Lazy ? _choicesCreator.Value : GetChoices();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Value"></param>
    /// <param name="Show"></param>
    public readonly record struct Pair(string Value, string Show)
    {
        /// <summary>
        /// Convert to pair from string.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator Pair(string value) => new (value, value);
    }

    /// <summary>
    /// 
    /// </summary>
    protected ChoicesAttribute()
    {
        _choicesCreator = new(GetChoices);
    }

    /// <summary>
    /// How to get the choices.
    /// </summary>
    /// <returns></returns>
    protected abstract Pair[] GetChoices();
}
