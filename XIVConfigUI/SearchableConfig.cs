using System.Reflection;

namespace XIVConfigUI;
public abstract class SearchableConfig
{
    public abstract bool ShowTooltip { get; }
    public abstract bool IsPropertyValid(PropertyInfo property);
    public abstract void PropertyInvalidTooltip(PropertyInfo property);
    public abstract void PreNameDrawing(PropertyInfo property);
}
