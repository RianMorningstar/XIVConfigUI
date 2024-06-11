using System.Collections;
using XIVConfigUI.Attributes;
using XIVConfigUI.SearchableConfigs;

namespace XIVConfigUI;

internal readonly record struct SearchPair(UIAttribute Attribute, Searchable Searchable);

/// <summary>
/// The filter for drawing configs.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly record struct FilterKey<T> where T : Enum
{
    /// <summary>
    /// The filter.
    /// </summary>
    public T Filter { get; init; }

    /// <summary>
    /// Before drawing.
    /// </summary>
    public Action? Before { get; init; }

    /// <summary>
    /// After drawing.
    /// </summary>
    public Action? After { get; init; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filter"></param>
    public static implicit operator FilterKey<T>(T filter) => new() { Filter = filter };
}

/// <summary>
/// The collections that can be serached.
/// </summary>
public class SearchableCollection : IDisposable, IEnumerable<Searchable>
{
    private readonly List<SearchPair> _items;

    internal static SearchableConfig? _searchableConfig;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="searchableConfig"></param>
    public SearchableCollection(object config, SearchableConfig? searchableConfig = null)
    {
        _searchableConfig = searchableConfig;
        var properties = config.GetType().GetRuntimeProperties();
        var count = properties.Count();
        var pairs = new List<SearchPair>(count);
        var parents = new Dictionary<string, CheckBoxSearch>(count);

        foreach (var property in properties)
        {
            var ui = property.GetCustomAttribute<UIAttribute>();
            if (ui == null) continue;

            var item = CreateSearchable(property);
            if (item == null) continue;

            pairs.Add(new(ui, item));

            if (item is not CheckBoxSearch search) continue;
            parents[property.Name] = search;
        }

        _items = new List<SearchPair>(pairs.Count);

        foreach (var pair in pairs)
        {
            var parentName = pair.Attribute.Parent;
            if (string.IsNullOrEmpty(parentName)
                || !parents.TryGetValue(parentName, out var parent))
            {
                _items.Add(pair);
                continue;
            }
            parent.AddChild(pair.Searchable);
        }

        XIVConfigUIMain._searchableCollections.Add(this);
        _searchableConfig = null;

        Searchable? CreateSearchable(PropertyInfo property)
        {
            var type = property.PropertyType;

            if (searchableConfig?.PropertyNameCreators?.TryGetValue(property.Name, out var func) ?? false)
            {
                return func(property);
            }
            else if (searchableConfig?.PropertyTypeCreators?.TryGetValue(type, out func) ?? false)
            {
                return func(property);
            }
            else if (type.IsEnum)
            {
                return new EnumSearch(property, config);
            }
            else if (type == typeof(bool))
            {
                return new CheckBoxSearch(property, config);
            }
            else if (type == typeof(float))
            {
                return new DragFloatSearch(property, config);
            }
            else if (type == typeof(int))
            {
                return new DragIntSearch(property, config);
            }
            else if (type == typeof(Vector2))
            {
                return new DragFloatRangeSearch(property, config);
            }
            else if (type == typeof(Vector2Int))
            {
                return new DragIntRangeSearch(property, config);
            }
            else if (type == typeof(Vector4))
            {
                var uiType = property.GetCustomAttribute<UITypeAttribute>()?.Type ?? UiType.Color;

                return uiType switch
                {
                    UiType.Padding => new PaddingSearch(property, config),
                    _ => new ColorEditSearch(property, config),
                };
            }
            else if (type == typeof(string))
            {
                return new InputTextSearch(property, config);
            }


#if DEBUG
            Service.Log.Warning($"Failed to create search item, the type is {type.Name}");
#endif
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="filters"></param>
    /// <returns></returns>
    public CollapsingHeaderGroup GetGroups<T>(params FilterKey<T>[] filters) where T : Enum
    {
        Dictionary<Func<string>, Action> dict = [];
        foreach (var filter in filters)
        {
            dict[() => filter.Filter.Local()] = () =>
            {
                filter.Before?.Invoke();
                DrawItems(Convert.ToInt32(filter.Filter));
                filter.After?.Invoke();
            };
        }
        return new (dict);
    }

    /// <summary>
    /// Draw the items based on the text filter.
    /// </summary>
    /// <param name="filter"></param>
    public void DrawItems(int filter)
    {
        bool isFirst = true;
        foreach (var grp in _items.Where(i => i.Attribute.Filter == filter)
            .GroupBy(i => i.Attribute.Section))
        {
            if (!isFirst)
            {
                ImGui.Separator();
            }
            foreach (var item in grp.OrderBy(i => i.Attribute.Order))
            {
                item.Searchable.Draw();
            }

            isFirst = false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        XIVConfigUIMain._searchableCollections.Remove(this);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public IEnumerator<Searchable> GetEnumerator()
    {
        return _items.Select(i => i.Searchable).SelectMany(GetChildren).GetEnumerator();
        static IEnumerable<Searchable> GetChildren(Searchable searchable)
        {
            var myself = new Searchable[] { searchable };
            if (searchable is CheckBoxSearch c && c.Children != null)
            {
                return c.Children.SelectMany(GetChildren).Union(myself);
            }
            else return myself;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
