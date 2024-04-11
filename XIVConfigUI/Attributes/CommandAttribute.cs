namespace XIVConfigUI.Attributes;

/// <summary>
/// The sub command for some config class.
/// </summary>
/// <param name="subCommand"><see cref="SubCommand"/></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CommandAttribute(string subCommand) : Attribute
{
    /// <summary>
    /// The sub command
    /// </summary>
    public string SubCommand => subCommand;
}
