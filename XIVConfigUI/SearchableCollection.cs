using XIVConfigUI.SearchableConfigs;

namespace XIVConfigUI;
internal readonly record struct SearchPair(UIAttribute Attribute, Searchable Searchable);

public readonly record struct FilterKey<T> where T : Enum
{
    public T Filter { get; init; }
    public Action? Before { get; init; }
    public Action? After { get; init; }

    public static implicit operator FilterKey<T>(T filter) => new() { Filter = filter };
}

/// <summary>
/// The collections that can be serached.
/// </summary>
public class SearchableCollection
{
    private readonly List<SearchPair> _items;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="propertyNameCreaters"></param>
    /// <param name="propertyTypeCreaters"></param>
    public SearchableCollection(object config, Dictionary<string, Func<PropertyInfo, Searchable>>? propertyNameCreaters = null,
        Dictionary<Type, Func<PropertyInfo, Searchable>>? propertyTypeCreaters = null)
    {
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

        Searchable? CreateSearchable(PropertyInfo property)
        {
            var type = property.PropertyType;

            if (propertyNameCreaters?.TryGetValue(property.Name, out var func) ?? false)
            {
                return func(property);
            }
            else if (propertyTypeCreaters?.TryGetValue(type, out func) ?? false)
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
                return new ColorEditSearch(property, config);
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

    private static readonly char[] _splitChar = [' ', ',', '、', '.', '。'];

    /// <summary>
    /// Search the items based on the <paramref name="searchingText"/>.
    /// </summary>
    /// <param name="searchingText"></param>
    /// <returns></returns>
    public Searchable[] SearchItems(string searchingText)
    {
        if (string.IsNullOrEmpty(searchingText)) return [];

        const int MAX_RESULT_LENGTH = 20;

        var results = new Searchable[MAX_RESULT_LENGTH];

        var enumerator = _items.Select(i => i.Searchable).SelectMany(GetChildren)
            .OrderByDescending(i => Similarity(i.SearchingKeys, searchingText))
            .Select(GetParent).GetEnumerator();

        int index = 0;
        while (enumerator.MoveNext() && index < MAX_RESULT_LENGTH)
        {
            if (results.Contains(enumerator.Current)) continue;
            results[index++] = enumerator.Current;
        }
        return results;


        static IEnumerable<Searchable> GetChildren(Searchable searchable)
        {
            var myself = new Searchable[] { searchable };
            if (searchable is CheckBoxSearch c && c.Children != null)
            {
                return c.Children.SelectMany(GetChildren).Union(myself);
            }
            else return myself;
        }

        static Searchable GetParent(Searchable searchable)
        {
            if (searchable.Parent == null) return searchable;
            return GetParent(searchable.Parent);
        }
    }

    /// <summary>
    /// The similarity of the two texts.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static float Similarity(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        var chars = text.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);
        var keys = key.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);

        var startWithCount = chars.Count(i => keys.Any(k => i.StartsWith(k, StringComparison.OrdinalIgnoreCase)));

        var containCount = chars.Count(i => keys.Any(k => i.Contains(k, StringComparison.OrdinalIgnoreCase)));

        return startWithCount * 3 + containCount;
    }
}
