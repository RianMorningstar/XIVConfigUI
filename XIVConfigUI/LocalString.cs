using System.ComponentModel;

namespace XIVConfigUI;
internal enum LocalString
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
}
