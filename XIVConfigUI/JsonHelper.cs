﻿using Newtonsoft.Json;

namespace XIVConfigUI;
public static class JsonHelper
{
    public static string SerializeObject(object? value)
    {
        return JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
        });
    }

    public static T? DeserializeObject<T>(string value)
    {
        return (T?)DeserializeObject(value, typeof(T));
    }

    public static object? DeserializeObject(string value, Type type)
    {
        return JsonConvert.DeserializeObject(value, type, new JsonSerializerSettings()
        {
            Converters = [GeneralJsonConverter.Instance],
            MissingMemberHandling = MissingMemberHandling.Error,
            Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            }
            !
        });
    }
}
