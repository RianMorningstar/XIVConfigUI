namespace XIVConfigUI.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CommandAttribute(string subCommand) : Attribute
{
    public string SubCommand => subCommand;
}
