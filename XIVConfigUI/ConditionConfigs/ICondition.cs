namespace XIVConfigUI.ConditionConfigs;

/// <summary>
/// For the list item which wanna show icon.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// The state of this condition.
    /// </summary>
    public bool? State { get; }
}
