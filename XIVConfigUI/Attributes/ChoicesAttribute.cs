namespace XIVConfigUI.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class ChoicesAttribute : Attribute
{
    protected virtual bool Lazy => true;
    private readonly Lazy<Pair[]> _choicesCreator;
    public Pair[] Choices => Lazy ? _choicesCreator.Value : GetChoices();
    public readonly record struct Pair(string Value, string Show);

    protected ChoicesAttribute()
    {
        _choicesCreator = new(GetChoices);
    }

    protected abstract Pair[] GetChoices();
}
