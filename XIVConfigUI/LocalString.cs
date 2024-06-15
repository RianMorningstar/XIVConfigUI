using System.ComponentModel;

namespace XIVConfigUI;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public enum LocalString
{
    [Description("Search Result")]
    Search_Result,

    [Description("Search... ")]
    Searching,

    [Description("Search")]
    Search,

    [Description("Reset to Default Value.")]
    ResetToDefault,

    [Description("Execute \"{0}\"")]
    ExecuteCommand,

    [Description("Copy \"{0}\"")]
    CopyCommand,

    [Description("Click to open the crowdin for modifying localization!")]
    Localization,

    [Description("Click to see the source code!")]
    SourceCode,

    [Description("Remove")]
    Remove,

    [Description("Move Up")]
    MoveUp,

    [Description("Move Down")]
    MoveDown,

    [Description("Copy to Clipboard")]
    CopyToClipboard,

    [Description("From Clipboard")]
    FromClipboard,

    [Description("List")]
    List,
}
