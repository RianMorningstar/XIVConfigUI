namespace XIVConfigUI.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ListUIAttribute(uint icon) : Attribute
{
    public uint Icon => icon;
    public string Description { get; set; }

    internal ListUIAttribute() : this(0)
    {

    }
    public virtual void OnClick(object obj) { }
}
