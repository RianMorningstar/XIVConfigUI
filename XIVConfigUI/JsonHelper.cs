using Newtonsoft.Json;

namespace XIVConfigUI;

/// <summary>
/// A general converter for json.
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// <see cref="JsonConvert.SerializeObject(object?)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string SerializeObject(object? value)
    {
        return JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
        });
    }

    /// <summary>
    /// <see cref="JsonConvert.DeserializeObject{T}(string)"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T? DeserializeObject<T>(string value)
    {
        return (T?)DeserializeObject(value, typeof(T));
    }

    /// <summary>
    /// <see cref="JsonConvert.DeserializeObject(string)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object? DeserializeObject(string value, Type type)
    {
        return JsonConvert.DeserializeObject(value, type, new JsonSerializerSettings()
        {
            Converters = [GeneralJsonConverter.Instance],
            MissingMemberHandling = MissingMemberHandling.Error,
            Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            }!
        });
    }
}
