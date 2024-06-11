namespace XIVConfigUI.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ListUIAttribute(uint icon) : Attribute
{
    public uint Icon => icon;
    public string Description { get; set; }
    public virtual void OnClick() { }
}
