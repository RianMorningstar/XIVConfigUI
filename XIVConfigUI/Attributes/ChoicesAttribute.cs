namespace XIVConfigUI.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class ChoicesAttribute : Attribute
{
    private readonly Lazy<Pair[]> _choicesCreator;
    public Pair[] Choices => _choicesCreator.Value;
    public readonly record struct Pair(string Value, string Show);

    protected ChoicesAttribute()
    {
        _choicesCreator = new(GetChoices);
    }

    protected abstract Pair[] GetChoices();
}
