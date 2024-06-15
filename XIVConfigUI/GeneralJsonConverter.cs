using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XIVConfigUI;

/// <summary>
/// A general Converter.
/// </summary>
public class GeneralJsonConverter : JsonConverter
{
    /// <summary>
    /// The instance.
    /// </summary>
    public static GeneralJsonConverter Instance { get; } = new();

    private GeneralJsonConverter()
    {
        
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsInterface || objectType.IsAbstract;
    }

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
    }

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        // Load JObject from stream
        JObject jObject = JObject.Load(reader);

        // Create target object based on JObject
        var target = Create(jObject, objectType);

        // Populate the object properties
        if (target != null)
        {
            serializer.Populate(jObject.CreateReader(), target);
        }

        return target;
    }

    private object? Create(JObject jObject, Type objectType)
    {
        foreach (var type in objectType.Assembly.GetTypes().Where(t =>
        {
            if (t.IsAbstract) return false;
            if (t.GetConstructor([]) == null) return false;
            return t.IsAssignableTo(objectType);
        }))
        {
            var propertiesName = GetTypeProperties(type);
            if (propertiesName.All(n => jObject[n] != null))
            {
                return Activator.CreateInstance(type);
            }
        }

        Service.Log.Error("Failed to convert the type from json: " + (objectType.FullName ?? objectType.Name));

        return null;
    }

    private string[] GetTypeProperties(Type type)
    {
        var fields = type.GetRuntimeFields();
        var fieldsName = fields.Where(f => f.GetCustomAttribute<JsonPropertyAttribute>() != null || f.GetCustomAttribute<JsonIgnoreAttribute>() == null && f.IsPublic)
            .Select(f => f.Name);

        var properties = type.GetRuntimeProperties();
        var propertiesName = properties.Where(p =>
        {
            var getter = p.GetMethod;
            var setter = p.SetMethod;
            if (setter == null || getter == null) return false;
            if (p.GetCustomAttribute<JsonPropertyAttribute>() != null) return true;
            if (p.GetCustomAttribute<JsonIgnoreAttribute>() != null) return false;
            return getter.IsPublic;
        }).Select(f => f.Name);

        return [.. fieldsName, .. propertiesName];
    }

    private static bool FieldExists(string fieldName, JObject jObject)
    {
        return jObject[fieldName] != null;
    }
}
