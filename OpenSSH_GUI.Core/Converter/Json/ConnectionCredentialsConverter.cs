#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:32

#endregion

using System.Text.Json;
using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Converter.Json;

public class ConnectionCredentialsConverter(DirectoryCrawler crawler) : JsonConverter<IConnectionCredentials>
{
    public override IConnectionCredentials Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;

        if (jsonObject.TryGetProperty("password", out _))
            return JsonSerializer.Deserialize<PasswordConnectionCredentials>(jsonObject.GetRawText(), options);

        if (jsonObject.TryGetProperty("key_file_path", out var path))
        {
            var obj = JsonSerializer.Deserialize<KeyConnectionCredentials>(jsonObject.GetRawText(), options);
            var found = crawler.GetAllKeys().FirstOrDefault(e => string.Equals(e.AbsoluteFilePath, path.GetString()));
            if (found is null)
                obj.RenewKey();
            else
                obj.Key = found;

            return obj;
        }

        var multi = JsonSerializer.Deserialize<MultiKeyConnectionCredentials>(jsonObject.GetRawText(), options);
        multi.Keys = crawler.GetAllKeys();
        return multi;
    }

    public override void Write(Utf8JsonWriter writer, IConnectionCredentials value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}